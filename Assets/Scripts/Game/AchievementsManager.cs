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
        [SerializeField] private AudioClip unlockClip; // 成就解锁音效
        [SerializeField] private GameObject achievementPopupPrefab; // Steam式成就弹窗预制体

        public AchievementDef[] Achievements => achievements;

        /// <summary>(成就定义, 奖励币) —— toast/UI 订阅。</summary>
        public event Action<AchievementDef, int> OnUnlocked;

        private readonly Queue<(AchievementDef def, int reward)> toastQueue = new Queue<(AchievementDef, int)>();
        private GameObject popupCard;
        private Image popupIcon;
        private TMP_Text popupCaption;
        private TMP_Text popupName;
        private CanvasGroup popupGroup;
        private AudioSource sfxSource;
        private Coroutine toastRoutine;
        private bool checking;
        private Transform canvasRoot;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            canvasRoot = FindObjectOfType<Canvas>().transform;
            if (PlayerStats.Instance != null) PlayerStats.Instance.OnStatsChanged += CheckAll;
            if (EconomyManager.Instance != null) EconomyManager.Instance.OnChanged += CheckAll;
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
                    if (toastRoutine == null) toastRoutine = StartCoroutine(PopupLoop());
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

        // ---------- 成就弹窗（Steam 式左下角卡片，预制体实例化） ----------

        private void BuildPopup()
        {
            // 音源
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;

            if (achievementPopupPrefab == null)
                Debug.LogWarning("[AchievementsManager] achievementPopupPrefab 未接线，成就弹窗无法显示");
        }

        private System.Collections.IEnumerator PopupLoop()
        {
            const float ShowX = 40f, OffX = 520f;
            const float slideIn = 0.35f, hold = 5.0f, slideOut = 0.3f, fade = 0.3f;

            while (toastQueue.Count > 0)
            {
                var (def, reward) = toastQueue.Dequeue();

                if (achievementPopupPrefab == null)
                {
                    Debug.LogWarning("[AchievementsManager] achievementPopupPrefab 未接线，跳过弹窗");
                    continue;
                }

                // 实例化预制体并填充数据
                popupCard = Instantiate(achievementPopupPrefab, canvasRoot);
                popupCard.name = "AchievementPopup";
                var rt = (RectTransform)popupCard.transform;
                rt.anchoredPosition = new Vector2(ShowX - OffX, 40f);

                popupIcon = popupCard.transform.Find("Icon")?.GetComponent<Image>();
                popupCaption = popupCard.transform.Find("Caption")?.GetComponent<TMPro.TMP_Text>();
                popupName = popupCard.transform.Find("Name")?.GetComponent<TMPro.TMP_Text>();
                popupGroup = popupCard.GetComponent<CanvasGroup>() ?? popupCard.AddComponent<CanvasGroup>();

                if (popupIcon != null)
                {
                    popupIcon.sprite = def.icon;
                    popupIcon.gameObject.SetActive(def.icon != null);
                }
                if (popupCaption != null) popupCaption.text = "成就达成";
                if (popupName != null) popupName.text = $"<b>{def.displayName}</b>  <color=#E6B84C>+{reward} 仙喵币</color>";

                if (unlockClip != null && sfxSource != null) sfxSource.PlayOneShot(unlockClip);

                // 滑入（左下角外 → 卡片位）
                float t = 0f;
                while (t < slideIn)
                {
                    t += Time.unscaledDeltaTime;
                    float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / slideIn), 3f);
                    rt.anchoredPosition = new Vector2(Mathf.Lerp(ShowX - OffX, ShowX, k), 40f);
                    popupGroup.alpha = Mathf.Clamp01(t / (fade * 0.7f));
                    yield return null;
                }
                rt.anchoredPosition = new Vector2(ShowX, 40f);
                popupGroup.alpha = 1f;

                yield return new WaitForSecondsRealtime(hold);

                // 滑出
                t = 0f;
                while (t < slideOut)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / slideOut);
                    rt.anchoredPosition = new Vector2(Mathf.Lerp(ShowX, ShowX - OffX, k * k), 40f);
                    popupGroup.alpha = 1f - k;
                    yield return null;
                }
                Destroy(popupCard);
                popupCard = null;
            }
            toastRoutine = null;
        }
    }
}
