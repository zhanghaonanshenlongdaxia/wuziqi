using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Wuziqi.Core;
using Wuziqi.Game;

namespace Wuziqi.UI
{
    /// <summary>
    /// 猫仙人对手：Animator 帧动画 + 对话气泡 + 局势情绪联动 + 连胜记录。
    /// 每只猫有独立的台词性格，切换猫时自动加载对应台词。
    /// </summary>
    public class CharacterController : MonoBehaviour
    {
        public enum Mood { Idle, Thinking, Smug, Worried, Celebrate, Defeat }

        [Header("引用")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private SpriteAnimator spriteAnimator;
        [SerializeField] private GameObject bubbleRoot;
        [SerializeField] private TMP_Text bubbleText;
        [SerializeField] private TMP_Text streakText;

        // ========== 每只猫的台词数据 ==========

        private struct CatLines
        {
            public string[] opening, openingStreak3, openingStreak5;
            public string[] thinking, aiStrong, aiGood;
            public string[] threatBig, threat;
            public string[] aiWin, playerWin, draw;
        }

        private Dictionary<string, CatLines> catDialogue;

        private void InitDialogue()
        {
            catDialogue = new Dictionary<string, CatLines>();

            catDialogue["小白"] = new CatLines
            {
                opening = new[] { "喵～请多指教！", "我会努力的！虽然不太会下棋…" },
                openingStreak3 = new[] { "赢了三场？我在做梦吧…", "好开心！尾巴摇得好快！" },
                openingStreak5 = new[] { "五连胜！我不是在做梦吧？！", "今天的我好像特别厉害！" },
                thinking = new[] { "嗯…这步棋好难呀…", "让我想想…猫咪的直觉告诉我…", "呜…脑袋要冒烟了…" },
                aiStrong = new[] { "嘿嘿，好像下了步好棋！", "这步棋连我自己都没想到！" },
                aiGood = new[] { "这样下应该可以吧？", "虽然不知道对不对，但感觉不错～" },
                threatBig = new[] { "呜呜…好像下错了…", "好厉害…我不会输吧？" },
                threat = new[] { "咦？这步棋好厉害…", "你的棋艺进步好快呀！" },
                aiWin = new[] { "赢了！耶耶耶！", "太好了太好了～喵！" },
                playerWin = new[] { "呜…你赢了…好厉害…", "下次我一定会更努力的！" },
                draw = new[] { "平局？也挺好的～", "和棋也是一种缘分呢！" },
            };

            catDialogue["橘座"] = new CatLines
            {
                opening = new[] { "嗯…先让本座打个哈欠…", "来吧，本座刚吃饱，正好消化一下。" },
                openingStreak3 = new[] { "三连胜？不错不错，值得加个鸡腿。", "本座的尾巴已经翘到天上去了～" },
                openingStreak5 = new[] { "五连胜？本座的威名已传遍天下！", "别急，让本座先舔舔爪子。" },
                thinking = new[] { "唔…这步棋得想想…肚子有点饿…", "让本座用吃鱼的智慧想想…", "嗯…困了…但棋不能输…" },
                aiStrong = new[] { "嘿嘿，本座可不是白吃那么多鱼的～", "这步棋，稳如老猫。" },
                aiGood = new[] { "还行吧，本座随手一下。", "嗯，本座的肚子和棋艺一样圆满。" },
                threatBig = new[] { "嘶…这棋有点棘手…", "本座的鱼干可能要保不住了…" },
                threat = new[] { "哟，有两下子嘛。", "嗯？你这步棋有点意思。" },
                aiWin = new[] { "承让承让，本座要去睡午觉了。", "赢了～今晚加餐！" },
                playerWin = new[] { "嗯…本座只是太饱了反应慢。", "下次本座空腹来，你就没机会了。" },
                draw = new[] { "平局？本座无所谓，反正有鱼吃。", "和棋也好，本座正好困了。" },
            };

            catDialogue["黑炭"] = new CatLines
            {
                opening = new[] { "…出招。", "不必多言，落子吧。" },
                openingStreak3 = new[] { "三场…尚可。", "连胜？不过是理所当然。" },
                openingStreak5 = new[] { "…五连胜？无趣。", "你的棋，还不够看。" },
                thinking = new[] { "…", "嗯。", "有意思。" },
                aiStrong = new[] { "…中。", "结束了。" },
                aiGood = new[] { "…还行。", "嗯。" },
                threatBig = new[] { "…!", "这步棋…有破绽。" },
                threat = new[] { "…哦？", "有点意思。" },
                aiWin = new[] { "…承让。", "你的棋…还差得远。" },
                playerWin = new[] { "…不错。", "下次。" },
                draw = new[] { "…平局。", "哼。" },
            };

            catDialogue["花斑"] = new CatLines
            {
                opening = new[] { "来来来！花斑大侠在此！", "准备好了吗？花斑要出招啦～" },
                openingStreak3 = new[] { "三连胜！花斑果然是天才！", "哈哈哈，花斑的爪子今天特别灵活！" },
                openingStreak5 = new[] { "五连胜！花斑要上天啦！", "有没有人来挑战花斑大侠？" },
                thinking = new[] { "嗯…花斑在想一个绝妙的招数！", "等等，花斑有个好主意！", "左思右想…不如出其不意！" },
                aiStrong = new[] { "哈哈！这招花斑练了好久！", "看到没？这就是花斑的厉害！" },
                aiGood = new[] { "不错不错，花斑今天手感火热！", "嘿嘿，这步棋花斑很满意～" },
                threatBig = new[] { "哎呀！这步棋花斑没料到！", "糟了糟了…花斑要认真了！" },
                threat = new[] { "咦？你也会这招？", "不错嘛，能逼花斑用这招！" },
                aiWin = new[] { "花斑赢啦！耶！", "哈哈，花斑大侠果然厉害！" },
                playerWin = new[] { "哼！花斑今天状态不好！", "下次花斑一定赢回来！" },
                draw = new[] { "平局？花斑觉得挺刺激的！", "再来一局！花斑还没玩够！" },
            };

            catDialogue["银渐层"] = new CatLines
            {
                opening = new[] { "请赐教。", "愿与阁下切磋一二。" },
                openingStreak3 = new[] { "连胜三场，尚在预料之中。", "不过是理所当然的结果。" },
                openingStreak5 = new[] { "五连胜？本猫的棋艺无需证明。", "无聊…有谁能与本猫一战？" },
                thinking = new[] { "容本猫思量片刻…", "此局…需从长计议。", "有趣，这步棋值得深思。" },
                aiStrong = new[] { "此招已在意料之中。", "本猫的每一步，皆有深意。" },
                aiGood = new[] { "尚可。", "本猫的棋路，岂是你能参透的。" },
                threatBig = new[] { "这步棋…有些出乎意料。", "容本猫重新审视此局。" },
                threat = new[] { "哦？阁下倒是有些棋力。", "这步棋，本猫认可。" },
                aiWin = new[] { "承让。本猫的胜利，毫无悬念。", "胜负已分，不必执着。" },
                playerWin = new[] { "…这盘棋，本猫记下了。", "阁下的棋艺，确实不凡。" },
                draw = new[] { "平局…倒也是个有趣的结果。", "此局旗鼓相当，改日再战。" },
            };

            catDialogue["玄猫"] = new CatLines
            {
                opening = new[] { "年轻人，老朽奉陪。", "棋盘之上，无长幼之分。" },
                openingStreak3 = new[] { "三连胜？不过是热身罢了。", "老朽的棋，你还嫩了点。" },
                openingStreak5 = new[] { "五连胜？老朽纵横棋坛数十年。", "年轻人，老朽不介意让你见识见识。" },
                thinking = new[] { "且慢…此局有变。", "老朽的棋路，岂是你能揣测的。", "嗯…这步棋，有意思。" },
                aiStrong = new[] { "此招雷霆万钧！", "老朽的棋，如暴风骤雨！" },
                aiGood = new[] { "还行，老朽尚未全力。", "这不过是老朽的三成实力。" },
                threatBig = new[] { "唔…这棋走得不错。", "年轻人，你的棋让老朽刮目相看。" },
                threat = new[] { "哦？有些本事。", "老朽小看你了。" },
                aiWin = new[] { "承让。棋道无涯，继续修炼吧。", "老朽的棋，你还差得远呢。" },
                playerWin = new[] { "…好棋。老朽心服口服。", "年轻人，你让老朽想起了当年。" },
                draw = new[] { "平局？倒是难得。", "此局旗鼓相当，改日再较高下。" },
            };

            catDialogue["仙喵长老"] = new CatLines
            {
                opening = new[] { "喵～施主，贫猫有礼了。", "棋盘如天地，落子如布阵。" },
                openingStreak3 = new[] { "三连胜？缘起缘灭，皆是定数。", "贫猫的尾巴，确实有点翘了。" },
                openingStreak5 = new[] { "五连胜？唉，无敌是多么寂寞。", "施主，你可知道什么是真正的棋道？" },
                thinking = new[] { "天道无常，棋道亦然…", "喵～让贫猫参悟一番…", "这步棋，暗合天机…", "唔…贫猫的胡子都竖起来了。" },
                aiStrong = new[] { "此乃天外飞仙之招！", "贫猫的棋，妙法自然。" },
                aiGood = new[] { "善哉善哉，贫猫随手一拈。", "这步棋，猫爪拈花。" },
                threatBig = new[] { "施主…你这步棋，有点东西。", "唔…贫猫的毛都炸了…" },
                threat = new[] { "哦？施主棋艺精进了不少。", "有趣有趣，这棋走得妙。" },
                aiWin = new[] { "承让承让，贫猫要去晒太阳了。", "喵～胜负乃兵家常事。" },
                playerWin = new[] { "施主好棋！贫猫心悦诚服。", "喵～今日棋兴已尽，改日再战。" },
                draw = new[] { "平局？此乃天意，妙不可言。", "施主，你与贫猫棋力相当呢。" },
            };
        }

        // ========== 运行时状态 ==========

        private Animator catAnimator;
        private Dictionary<string, AnimatorOverrideController> overrideControllers;
        private string currentCatName;
        private CatLines currentLines;
        private Mood currentMood = Mood.Idle;
        private Mood pendingMood;
        private bool hasPending;
        private float moodHoldUntil;
        private Coroutine bubbleRoutine;
        private float lastBubbleEnd = -10f;
        private const float BubbleCooldown = 2.5f;

        private void Start()
        {
            InitDialogue();

            if (gameManager == null) gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.StonePlaced += OnStonePlaced;
                gameManager.PlayerTurnChanged += OnTurnChanged;
                gameManager.BoardReset += OnBoardReset;
                gameManager.GameEnded += OnGameEnded;
            }

            if (bubbleRoot != null) bubbleRoot.SetActive(false);
            UpdateStreakText();

            // 初始化 Animator（在 SpriteAnimator 所在的 GameObject 上）
            if (spriteAnimator != null)
                catAnimator = spriteAnimator.GetComponent<Animator>();

            // 加载所有猫的 OverrideController
            overrideControllers = new Dictionary<string, AnimatorOverrideController>();
            string[] catNames = { "小白", "橘座", "黑炭", "花斑", "银渐层", "玄猫", "仙喵长老" };
            foreach (var catName in catNames)
            {
                var oc = Resources.Load<AnimatorOverrideController>("Anim/" + catName);
                if (oc != null) overrideControllers[catName] = oc;
            }

            // 加载当前猫
            string cat = GetCurrentCatName();
            currentCatName = cat;
            currentLines = GetLines(cat);
            LoadCatAndApply(cat);

            SetMood(Mood.Idle);
            ShowBubble(PickLine(currentLines.opening), 3f, true);

            if (CatManager.Instance != null)
                CatManager.Instance.OnCatChanged += OnCatChanged;
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.StonePlaced -= OnStonePlaced;
                gameManager.PlayerTurnChanged -= OnTurnChanged;
                gameManager.BoardReset -= OnBoardReset;
                gameManager.GameEnded -= OnGameEnded;
            }
            if (CatManager.Instance != null)
                CatManager.Instance.OnCatChanged -= OnCatChanged;
        }

        // ========== 台词获取 ==========

        private CatLines GetLines(string catName)
        {
            if (catDialogue != null && catDialogue.TryGetValue(catName, out var lines))
                return lines;
            if (catDialogue != null && catDialogue.TryGetValue("小白", out var fallback))
                return fallback;
            return default;
        }

        private string GetCurrentCatName()
        {
            if (CatManager.Instance != null && CatManager.Instance.Selected != null)
                return CatManager.Instance.Selected.catName;
            return "小白";
        }

        // ========== 猫切换 ==========

        private void OnCatChanged(int newIndex)
        {
            var cat = CatManager.Instance.GetCat(newIndex);
            if (cat == null) return;

            currentCatName = cat.catName;
            currentLines = GetLines(currentCatName);
            LoadCatAndApply(currentCatName);

            SetMood(Mood.Idle);
        }

        // ========== 资源加载 ==========

        private void LoadCatAndApply(string catName)
        {
            // 切换 OverrideController
            if (overrideControllers.TryGetValue(catName, out var oc) && catAnimator != null)
                catAnimator.runtimeAnimatorController = oc;

            if (spriteAnimator == null) return;

            // 加载帧数据到 SpriteAnimator
            string[] moodNames = { "idle", "thinking", "smug", "celebrate", "defeat", "worried" };
            for (int i = 0; i < moodNames.Length; i++)
            {
                string path = "CatFrames/" + catName + "/" + moodNames[i];
                var sprites = Resources.LoadAll<Sprite>(path);
                if (sprites != null && sprites.Length > 0)
                {
                    var sorted = new System.Collections.Generic.List<Sprite>(sprites);
                    sorted.Sort((a, b) => a.name.CompareTo(b.name));
                    spriteAnimator.SetFrames(i, sorted.ToArray());
                }
            }
        }

        // ========== Animator 动画控制 ==========

        private void SetMood(Mood mood, float hold = 0f)
        {
            currentMood = mood;

            if (spriteAnimator != null)
                spriteAnimator.SetMood((int)mood);
            else if (catAnimator != null && catAnimator.runtimeAnimatorController != null)
                catAnimator.SetInteger("Mood", (int)mood);

            if (hold > 0f) moodHoldUntil = Time.time + hold;
        }

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

        // ========== 回调 ==========

        private void OnStonePlaced(Vector2Int cell, StoneColor color)
        {
            if (color == gameManager.playerColor)
            {
                long threat = GomokuAI.EvaluateThreat(gameManager.Board, cell.x, cell.y, color);
                if (threat >= GomokuAI.FourScore)
                {
                    TrySetMood(Mood.Worried);
                    ShowBubble(PickLine(currentLines.threatBig), 3f);
                }
                else if (threat >= GomokuAI.OpenThreeScore)
                {
                    TrySetMood(Mood.Thinking);
                    ShowBubble(PickLine(currentLines.threat), 3f);
                }
            }
            else
            {
                long threat = GomokuAI.EvaluateThreat(gameManager.Board, cell.x, cell.y, color);
                if (threat >= GomokuAI.FourScore)
                {
                    TrySetMood(Mood.Smug);
                    ShowBubble(PickLine(currentLines.aiStrong), 3f);
                }
                else
                {
                    TrySetMood(Mood.Thinking);
                    ShowBubble(PickLine(currentLines.aiGood), 3f);
                }
            }
        }

        private void OnTurnChanged(bool isPlayerTurn)
        {
            if (isPlayerTurn)
                TrySetMood(Mood.Idle);
            else
            {
                TrySetMood(Mood.Thinking);
                ShowBubble(PickLine(currentLines.thinking), 3f, true);
            }
        }

        private void OnBoardReset()
        {
            SetMood(Mood.Idle);
            ShowBubble(PickLine(currentLines.opening), 3f, true);
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
                ShowBubble(PickLine(currentLines.playerWin), 3.5f, true);
                WinStreak.Add();
            }
            else if (aiWon)
            {
                SetMood(Mood.Celebrate);
                ShowBubble(PickLine(currentLines.aiWin), 3.5f, true);
                WinStreak.Reset();
            }
            else
            {
                SetMood(Mood.Worried);
                ShowBubble(PickLine(currentLines.draw), 3f, true);
            }
            UpdateStreakText();
        }

        private void Update()
        {
            if (hasPending && Time.time >= moodHoldUntil)
            {
                SetMood(pendingMood);
                hasPending = false;
            }
        }

        // ========== 气泡 ==========

        public void ShowBubble(string text, float duration = 2.5f, bool force = false)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (!force && Time.time < lastBubbleEnd + BubbleCooldown) return;
            if (bubbleRoutine != null) StopCoroutine(bubbleRoutine);
            bubbleRoutine = StartCoroutine(BubbleRoutine(text, duration));
        }

        private IEnumerator BubbleRoutine(string text, float duration)
        {
            if (bubbleText != null) bubbleText.text = text;
            if (bubbleRoot != null) bubbleRoot.SetActive(true);
            lastBubbleEnd = Time.time + duration;
            yield return new WaitForSeconds(duration);
            if (bubbleRoot != null) bubbleRoot.SetActive(false);
            lastBubbleEnd = Time.time;
        }

        // ========== 工具 ==========

        private static string PickLine(string[] lines)
        {
            if (lines == null || lines.Length == 0) return null;
            return lines[Random.Range(0, lines.Length)];
        }

        private void UpdateStreakText()
        {
            if (streakText == null) return;
            int streak = WinStreak.Get();
            streakText.gameObject.SetActive(streak > 0);
            streakText.text = $"\u8fde\u80dc \u00d7{streak}";
        }

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
