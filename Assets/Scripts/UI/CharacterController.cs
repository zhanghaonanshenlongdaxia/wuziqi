using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Wuziqi.Core;
using Wuziqi.Game;

namespace Wuziqi.UI
{
    /// <summary>猫仙人对手：序列帧表情状态机、对话气泡、局势情绪联动、连胜记录。</summary>
    public class CharacterController : MonoBehaviour
    {
        public enum Mood { Idle, Thinking, Smug, Worried, Celebrate, Defeat }

        [Header("引用")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Image portrait;
        [SerializeField] private GameObject bubbleRoot;
        [SerializeField] private TMP_Text bubbleText;
        [SerializeField] private TMP_Text streakText;
        [SerializeField] private Sprite fallbackPortrait;

        [Header("动画帧（每组16帧）")]
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] thinkingFrames;
        [SerializeField] private Sprite[] smugFrames;
        [SerializeField] private Sprite[] worriedFrames;
        [SerializeField] private Sprite[] celebrateFrames;
        [SerializeField] private Sprite[] defeatFrames;
        [SerializeField, Range(4f, 15f)] private float frameRate = 8f;

        [Header("台词库")]
        [SerializeField, TextArea] private string[] openingLines = { "哼，本仙猫让你三子。", "来来来，本座倒要看看你的棋力。" };
        [SerializeField, TextArea] private string[] openingLinesStreak3 = { "连胜三场，尾巴要翘上天了？" };
        [SerializeField, TextArea] private string[] openingLinesStreak5 = { "又是你？！这次本仙猫绝不留情！" };
        [SerializeField, TextArea] private string[] thinkingLines = { "让我想想…", "唔…这步棋有玄机…" };
        [SerializeField, TextArea] private string[] aiStrongLines = { "接招！", "看好了，这就是仙家的实力～" };
        [SerializeField, TextArea] private string[] aiGoodLines = { "嘿嘿，妙不可言～", "本座的棋，天衣无缝。" };
        [SerializeField, TextArea] private string[] playerThreatBigLines = { "不、不妙…", "这不可能！" };
        [SerializeField, TextArea] private string[] playerThreatLines = { "咦？有两下子。", "小瞧你了…" };
        [SerializeField, TextArea] private string[] aiWinLines = { "承让承让～喵", "本仙猫宝刀未老！" };
        [SerializeField, TextArea] private string[] playerWinLines = { "哼！今日状态不佳，不算不算。", "本座…只是让着你！" };
        [SerializeField, TextArea] private string[] drawLines = { "平局？有趣，你有些长进。" };

        private Mood currentMood = Mood.Idle;
        private Sprite[] currentFrames;
        private int frameIndex;
        private float frameTimer;
        private Mood pendingMood;
        private bool hasPending;
        private float moodHoldUntil;
        private Coroutine bubbleRoutine;
        private float lastBubbleEnd = -10f;
        private const float BubbleCooldown = 2.5f;

        private string currentFramesDir;


        private void Start()
        {
            if (gameManager == null) gameManager = GameManager.Instance;
            gameManager.StonePlaced += OnStonePlaced;
            gameManager.PlayerTurnChanged += OnTurnChanged;
            gameManager.BoardReset += OnBoardReset;
            gameManager.GameEnded += OnGameEnded;

            bubbleRoot.SetActive(false);
            UpdateStreakText();

            // Load frames from CatManager if available, otherwise use serialized arrays
            if (CatManager.Instance != null && CatManager.Instance.Selected != null)
                ReloadFrames(CatManager.Instance.Selected.framesDir);

            SetMood(Mood.Idle);
            ShowBubble(PickLine(OpeningLinesForStreak(WinStreak.Get())), 3f, true);

            // Subscribe to cat changes
            if (CatManager.Instance != null)
                CatManager.Instance.OnCatChanged += OnCatChanged;
        }

        private void OnDestroy()
        {
            if (gameManager == null) return;
            gameManager.StonePlaced -= OnStonePlaced;
            gameManager.PlayerTurnChanged -= OnTurnChanged;
            gameManager.BoardReset -= OnBoardReset;
            gameManager.GameEnded -= OnGameEnded;
            if (CatManager.Instance != null)
                CatManager.Instance.OnCatChanged -= OnCatChanged;
        }

        // ---------- 猫猫切换 ----------

        private void OnCatChanged(int newIndex)
        {
            var cat = CatManager.Instance.GetCat(newIndex);
            if (cat != null)
                ReloadFrames(cat.framesDir);
            SetMood(Mood.Idle);
        }

        private void ReloadFrames(string framesDir)
        {
            if (string.IsNullOrEmpty(framesDir)) return;
            currentFramesDir = framesDir;

            // Support two layouts:
            // 1. Per-cat: Frames/{framesDir}/{mood}  (multi-cat future)
            // 2. Flat:    Frames/{mood}              (current single-cat, framesDir=="idle" fallback)
            idleFrames      = LoadFrameDir(framesDir, "idle")      ?? LoadFlatDir("idle");
            thinkingFrames  = LoadFrameDir(framesDir, "thinking")  ?? LoadFlatDir("thinking");
            smugFrames      = LoadFrameDir(framesDir, "smug")      ?? LoadFlatDir("smug");
            worriedFrames   = LoadFrameDir(framesDir, "worried")   ?? LoadFlatDir("worried");
            celebrateFrames = LoadFrameDir(framesDir, "celebrate") ?? LoadFlatDir("celebrate");
            defeatFrames    = LoadFrameDir(framesDir, "defeat")    ?? LoadFlatDir("defeat");
        }

        private static Sprite[] LoadFlatDir(string mood)
        {
            string path = $"Assets/Art/Cat/Frames/{mood}";
            var guids = UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { path });
            if (guids == null || guids.Length == 0) return null;
            var sprites = new System.Collections.Generic.List<Sprite>();
            foreach (var g in guids)
            {
                var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (s != null) sprites.Add(s);
            }
            sprites.Sort((a, b) => a.name.CompareTo(b.name));
            return sprites.Count > 0 ? sprites.ToArray() : null;
        }

        private static Sprite[] LoadFrameDir(string catDir, string mood)
        {
            string path = $"Assets/Art/Cat/Frames/{catDir}/{mood}";
            if (!UnityEditor.AssetDatabase.IsValidFolder(path)) return null;
            var guids = UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { path });
            if (guids == null || guids.Length == 0) return null;
            var sprites = new System.Collections.Generic.List<Sprite>();
            foreach (var g in guids)
            {
                var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (s != null) sprites.Add(s);
            }
            sprites.Sort((a, b) => a.name.CompareTo(b.name));
            return sprites.Count > 0 ? sprites.ToArray() : null;
        }

        // ---------- 事件处理 ----------

        private void OnStonePlaced(Vector2Int cell, StoneColor color)
        {
            StoneColor aiColor = GomokuAI.Other(gameManager.playerColor);
            long threat = GomokuAI.EvaluateThreat(gameManager.Board, cell.x, cell.y, color);

            if (color == aiColor)
            {
                if (threat >= GomokuAI.FourScore)
                {
                    SetMood(Mood.Smug);
                    ShowBubble(PickLine(aiStrongLines));
                }
                else if (threat >= GomokuAI.OpenThreeScore)
                {
                    SetMood(Mood.Smug);
                    ShowBubble(PickLine(aiGoodLines));
                }
            }
            else // 玩家落子
            {
                if (threat >= GomokuAI.OpenFourScore)
                {
                    SetMood(Mood.Worried, 1.1f);
                    ShowBubble(PickLine(playerThreatBigLines));
                }
                else if (threat >= GomokuAI.OpenThreeScore)
                {
                    SetMood(Mood.Worried, 1.1f);
                    ShowBubble(PickLine(playerThreatLines));
                }
            }
        }

        private void OnTurnChanged(bool isPlayerTurn)
        {
            if (!isPlayerTurn)
            {
                TrySetMood(Mood.Thinking);
                if (Random.value < 0.35f) ShowBubble(PickLine(thinkingLines));
            }
            else TrySetMood(Mood.Idle);
        }

        private void OnBoardReset()
        {
            hasPending = false;
            moodHoldUntil = 0f;
            SetMood(Mood.Idle);
            UpdateStreakText();
            ShowBubble(PickLine(OpeningLinesForStreak(WinStreak.Get())), 3f, true);
        }

        private void OnGameEnded(GameResult result, IReadOnlyList<Vector2Int> line)
        {
            bool playerWon = (result == GameResult.BlackWin && gameManager.playerColor == StoneColor.Black)
                          || (result == GameResult.WhiteWin && gameManager.playerColor == StoneColor.White);
            bool aiWon = (result == GameResult.BlackWin && gameManager.playerColor == StoneColor.White)
                       || (result == GameResult.WhiteWin && gameManager.playerColor == StoneColor.Black);

            if (playerWon)
            {
                SetMood(Mood.Defeat);
                ShowBubble(PickLine(playerWinLines), 3.5f, true);
                WinStreak.Add();
            }
            else if (aiWon)
            {
                SetMood(Mood.Celebrate);
                ShowBubble(PickLine(aiWinLines), 3.5f, true);
                WinStreak.Reset();
            }
            else
            {
                SetMood(Mood.Worried);
                ShowBubble(PickLine(drawLines), 3f, true);
            }
            UpdateStreakText();
        }

        // ---------- 情绪与动画 ----------

        private void SetMood(Mood mood, float hold = 0f)
        {
            currentMood = mood;
            currentFrames = FramesFor(mood);
            frameIndex = 0;
            frameTimer = 0f;
            if (hold > 0f) moodHoldUntil = Time.time + hold;
        }

        /// <summary>可延迟切换：当前情绪保持中则挂起，保持结束后自动切换。</summary>
        private void TrySetMood(Mood mood)
        {
            if (Time.time < moodHoldUntil)
            {
                pendingMood = mood;
                hasPending = true;
                return;
            }
            SetMood(mood);
        }

        private Sprite[] FramesFor(Mood mood)
        {
            switch (mood)
            {
                case Mood.Thinking: return HasFrames(thinkingFrames) ? thinkingFrames : idleFrames;
                case Mood.Smug: return HasFrames(smugFrames) ? smugFrames : idleFrames;
                case Mood.Worried: return HasFrames(worriedFrames) ? worriedFrames : idleFrames;
                case Mood.Celebrate: return HasFrames(celebrateFrames) ? celebrateFrames : idleFrames;
                case Mood.Defeat: return HasFrames(defeatFrames) ? defeatFrames : idleFrames;
                default: return idleFrames;
            }
        }

        private static bool HasFrames(Sprite[] frames) => frames != null && frames.Length > 0;

        private void Update()
        {
            if (hasPending && Time.time >= moodHoldUntil)
            {
                SetMood(pendingMood);
                hasPending = false;
            }

            if (currentFrames == null || currentFrames.Length == 0)
            {
                if (portrait != null && portrait.sprite == null && fallbackPortrait != null)
                    portrait.sprite = fallbackPortrait;
                return;
            }

            frameTimer += Time.deltaTime;
            float interval = 1f / frameRate;
            while (frameTimer >= interval)
            {
                frameTimer -= interval;
                frameIndex = (frameIndex + 1) % currentFrames.Length;
            }
            if (portrait != null) portrait.sprite = currentFrames[frameIndex];
        }

        // ---------- 气泡 ----------

        public void ShowBubble(string text, float duration = 2.5f, bool force = false)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (!force && Time.time < lastBubbleEnd + BubbleCooldown) return;
            if (bubbleRoutine != null) StopCoroutine(bubbleRoutine);
            bubbleRoutine = StartCoroutine(BubbleRoutine(text, duration));
        }

        private IEnumerator BubbleRoutine(string text, float duration)
        {
            bubbleText.text = text;
            bubbleRoot.SetActive(true);
            lastBubbleEnd = Time.time + duration;
            yield return new WaitForSeconds(duration);
            bubbleRoot.SetActive(false);
            lastBubbleEnd = Time.time;
        }

        // ---------- 工具 ----------

        private static string PickLine(string[] lines)
        {
            if (lines == null || lines.Length == 0) return null;
            return lines[Random.Range(0, lines.Length)];
        }

        private string[] OpeningLinesForStreak(int streak)
        {
            if (streak >= 5) return openingLinesStreak5;
            if (streak >= 3) return openingLinesStreak3;
            return openingLines;
        }

        private void UpdateStreakText()
        {
            if (streakText == null) return;
            int streak = WinStreak.Get();
            streakText.gameObject.SetActive(streak > 0);
            streakText.text = $"连胜 ×{streak}";
        }

        /// <summary>连胜记录（PlayerPrefs 持久化）。</summary>
        public static class WinStreak
        {
            private const string Key = "Wuziqi.WinStreak";

            public static int Get() => PlayerPrefs.GetInt(Key, 0);

            public static int Add()
            {
                int v = Get() + 1;
                PlayerPrefs.SetInt(Key, v);
                PlayerPrefs.Save();
                return v;
            }

            public static void Reset()
            {
                PlayerPrefs.SetInt(Key, 0);
                PlayerPrefs.Save();
            }
        }
    }
}


