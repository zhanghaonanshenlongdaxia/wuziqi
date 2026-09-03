using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Wuziqi.UI;

namespace Wuziqi.Game
{
    /// <summary>成就判定与发奖：监听统计变化，达成即发仙喵币并弹 toast。挂 GameManagers 节点。</summary>
    public class AchievementsManager : MonoBehaviour
    {
        public static AchievementsManager Instance { get; private set; }

        [SerializeField] private AchievementDef[] achievements;
        [SerializeField] private TMP_FontAsset toastFont;
        [SerializeField] private Sprite toastBgSprite; // BgButtonMedium 横幅底

        public AchievementDef[] Achievements => achievements;

        /// <summary>(成就定义, 奖励币) —— toast/UI 订阅。</summary>
        public event Action<AchievementDef, int> OnUnlocked;

        private readonly Queue<(AchievementDef def, int reward)> toastQueue = new Queue<(AchievementDef, int)>();
        private GameObject toastBanner;
        private TMP_Text toastText;
        private CanvasGroup toastGroup;
        private Coroutine toastRoutine;
        private bool checking;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (PlayerStats.Instance != null) PlayerStats.Instance.OnStatsChanged += CheckAll;
            if (EconomyManager.Instance != null) EconomyManager.Instance.OnChanged += CheckAll;
            BuildToast();
            CheckAll(); // 启动时补检（离线达成/版本更新遗漏）
        }

        private void OnDestroy()
        {
            if (PlayerStats.Instance != null) PlayerStats.Instance.OnStatsChanged -= CheckAll;
            if (EconomyManager.Instance != null) EconomyManager.Instance.OnChanged -= CheckAll;
            if (Instance == this) Instance = null;
        }

        /// <summary>成就当前进度（面板进度条用）。</summary>
        public int GetProgress(AchievementDef def)
        {
            var ps = PlayerStats.Instance;
            if (ps == null) return 0;
            switch (def.type)
            {
                case AchType.TotalWins: return ps.TotalWins;
                case AchType.MaxStreak: return ps.MaxStreak;
                case AchType.TotalGames: return ps.TotalGames;
                case AchType.CollectCats: return CountUnlockedCats();
                case AchType.SameCatGames: return ps.GetBestCatGames();
                case AchType.BeatAllCats: return CountBeatenCats();
                case AchType.CoinEarned: return ps.CoinEarnedTotal;
                case AchType.LoseStreak: return ps.CurrentLoseStreak;
                case AchType.SongUnlock: return CountUnlockedSongs();
                default: return 0;
            }
        }

        /// <summary>全量检查未达成成就，达成即发奖。</summary>
        public void CheckAll()
        {
            var ps = PlayerStats.Instance;
            if (ps == null || achievements == null || checking) return;
            checking = true;
            try
            {
                foreach (var def in achievements)
                {
                    if (def == null || ps.IsAchUnlocked(def.achId)) continue;
                    if (GetProgress(def) < def.targetValue) continue;

                    // 先发币后标记：崩溃窗口最坏重复领一次（玩家有利），见设计手册
                    if (EconomyManager.Instance != null) EconomyManager.Instance.AddCoins(def.rewardCoins);
                    ps.MarkAchUnlocked(def.achId);
                    ps.AddCoinEarned(def.rewardCoins); // 成就奖励也算累计收入

                    toastQueue.Enqueue((def, def.rewardCoins));
                    OnUnlocked?.Invoke(def, def.rewardCoins);
                    if (toastRoutine == null) toastRoutine = StartCoroutine(ToastLoop());
                }
            }
            finally { checking = false; }
        }

        private int CountUnlockedCats()
        {
            var cm = CatManager.Instance;
            if (cm == null) return 0;
            int n = 0;
            for (int i = 0; i < cm.CatCount; i++) if (cm.IsUnlocked(i)) n++;
            return n;
        }

        private int CountBeatenCats()
        {
            var cm = CatManager.Instance;
            if (cm == null || PlayerStats.Instance == null) return 0;
            int n = 0;
            for (int i = 0; i < cm.CatCount; i++)
            {
                var c = cm.GetCat(i);
                if (c != null && PlayerStats.Instance.GetCatWins(c.catName) > 0) n++;
            }
            return n;
        }

        private static int CountUnlockedSongs()
        {
            int n = 0;
            for (int i = 0; i < 9; i++) if (SongListPanel.IsSongUnlocked(i)) n++;
            return n;
        }

        // ---------- Toast ----------

        private void BuildToast()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            toastBanner = new GameObject("AchievementToast", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            var rt = (RectTransform)toastBanner.transform;
            rt.SetParent(canvas.transform, false);
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -96f);
            rt.sizeDelta = new Vector2(880f, 76f);

            var img = toastBanner.GetComponent<Image>();
            img.sprite = toastBgSprite;
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;

            toastGroup = toastBanner.GetComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastGroup.blocksRaycasts = false;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            var trt = (RectTransform)textGo.transform;
            trt.SetParent(rt, false);
            trt.offsetMin = new Vector2(24f, 8f);
            trt.offsetMax = new Vector2(-24f, -8f);
            toastText = textGo.GetComponent<TextMeshProUGUI>();
            toastText.font = toastFont;
            toastText.fontSize = 30;
            toastText.color = new Color(0.23f, 0.22f, 0.20f);
            toastText.alignment = TextAlignmentOptions.Center;
            toastText.raycastTarget = false;

            toastBanner.SetActive(false);
        }

        private System.Collections.IEnumerator ToastLoop()
        {
            const float fade = 0.25f, hold = 2.0f;
            while (toastQueue.Count > 0)
            {
                var (def, reward) = toastQueue.Dequeue();
                toastText.text = $"成就达成：{def.displayName}  +{reward} 仙喵币";
                toastBanner.SetActive(true);

                float t = 0f;
                while (t < fade) { t += Time.unscaledDeltaTime; toastGroup.alpha = t / fade; yield return null; }
                toastGroup.alpha = 1f;
                yield return new WaitForSecondsRealtime(hold);
                t = 0f;
                while (t < fade) { t += Time.unscaledDeltaTime; toastGroup.alpha = 1f - t / fade; yield return null; }
                toastBanner.SetActive(false);
            }
            toastRoutine = null;
        }
    }
}
