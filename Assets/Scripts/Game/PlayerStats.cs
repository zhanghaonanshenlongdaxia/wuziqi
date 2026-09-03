using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Wuziqi.Core;

namespace Wuziqi.Game
{
    /// <summary>玩家战绩统计：JSON 持久化，成就系统的数据源。挂 GameManagers 节点。</summary>
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        [Serializable]
        public class CatStat { public string catName; public int games; public int wins; }

        [Serializable]
        private class SaveData
        {
            public int totalGames;
            public int totalWins;
            public int currentStreak;
            public int maxStreak;
            public int currentLoseStreak;
            public int coinEarnedTotal;
            public List<CatStat> catStats = new List<CatStat>();
            public List<string> unlockedIds = new List<string>();
        }

        private const int MinValidMoves = 10; // 有效对局门槛：下满 10 手

        public int TotalGames { get; private set; }
        public int TotalWins { get; private set; }
        public int CurrentStreak { get; private set; }
        public int MaxStreak { get; private set; }
        public int CurrentLoseStreak { get; private set; }
        public int CoinEarnedTotal { get; private set; }

        /// <summary>任何统计变化（含成就解锁标记）后触发。</summary>
        public event Action OnStatsChanged;

        private readonly List<CatStat> catStats = new List<CatStat>();
        private readonly HashSet<string> unlockedIds = new HashSet<string>();
        private GameManager gameManager;

        private string SavePath => Path.Combine(Application.persistentDataPath, "records", "player_stats.json");

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Load();
        }

        private void Start()
        {
            if (gameManager == null) gameManager = GameManager.Instance;
            if (gameManager != null) gameManager.GameEnded += OnGameEnded;
        }

        private void OnDestroy()
        {
            if (gameManager != null) gameManager.GameEnded -= OnGameEnded;
            if (Instance == this) Instance = null;
        }

        // ---------- 累计 ----------

        private void OnGameEnded(GameResult result, IReadOnlyList<Vector2Int> line)
        {
            if (gameManager == null) return;
            bool playerWon = (result == GameResult.BlackWin && gameManager.playerColor == StoneColor.Black)
                          || (result == GameResult.WhiteWin && gameManager.playerColor == StoneColor.White);
            bool draw = result == GameResult.Draw;
            string catName = CatManager.Instance != null && CatManager.Instance.Selected != null
                ? CatManager.Instance.Selected.catName : "Unknown";
            Apply(catName, playerWon, draw, gameManager.Board.MoveCount);
        }

        /// <summary>累计一局结果（GameEnded 调用；公开以便回放/测试注入）。</summary>
        public void RecordGameResult(GameResult result, int moveCount, string catName)
        {
            bool playerWon = (result == GameResult.BlackWin && gameManager != null && gameManager.playerColor == StoneColor.Black)
                          || (result == GameResult.WhiteWin && gameManager != null && gameManager.playerColor == StoneColor.White);
            Apply(catName, playerWon, result == GameResult.Draw, moveCount);
        }

        private void Apply(string catName, bool won, bool draw, int moveCount)
        {
            if (moveCount < MinValidMoves) return; // 无效对局：不计入任何统计

            TotalGames++;
            var cs = GetOrCreate(catName);
            cs.games++;

            if (won)
            {
                TotalWins++;
                CurrentStreak++;
                CurrentLoseStreak = 0;
                MaxStreak = Mathf.Max(MaxStreak, CurrentStreak);
                cs.wins++;
            }
            else if (!draw)
            {
                CurrentStreak = 0;
                CurrentLoseStreak++;
            }
            // 平局：连胜/连败均保持不变

            Save();
            OnStatsChanged?.Invoke();
        }

        /// <summary>累计获得的仙喵币（只计收入；发币方调用）。</summary>
        public void AddCoinEarned(int amount)
        {
            if (amount <= 0) return;
            CoinEarnedTotal += amount;
            Save();
            OnStatsChanged?.Invoke();
        }

        // ---------- 成就解锁记录 ----------

        public bool IsAchUnlocked(string achId) => unlockedIds.Contains(achId);

        /// <summary>标记成就已解锁（先发币后调用本方法，崩溃容错见设计手册）。</summary>
        public void MarkAchUnlocked(string achId)
        {
            if (!unlockedIds.Add(achId)) return;
            Save();
            OnStatsChanged?.Invoke();
        }

        // ---------- GM（仅编辑器 GM 面板调用） ----------

        /// <summary>GM：灌满统计与每猫战绩（触发全部成就达成链路，用于验收弹窗/发币）。</summary>
        public void GM_FillAll()
        {
            TotalGames = 999;
            TotalWins = 999;
            CurrentStreak = 0;
            MaxStreak = 99;
            CurrentLoseStreak = 0;
            CoinEarnedTotal = 1000;
            catStats.Clear();
            var cm = CatManager.Instance;
            if (cm != null)
                for (int i = 0; i < cm.CatCount; i++)
                {
                    var c = cm.GetCat(i);
                    if (c != null) catStats.Add(new CatStat { catName = c.catName, games = 99, wins = 99 });
                }
            Save();
            OnStatsChanged?.Invoke();
        }

        /// <summary>GM：清空全部统计与成就解锁记录（成就可重新达成，用于复看效果）。</summary>
        public void GM_ResetAll()
        {
            TotalGames = 0; TotalWins = 0;
            CurrentStreak = 0; MaxStreak = 0; CurrentLoseStreak = 0;
            CoinEarnedTotal = 0;
            catStats.Clear();
            unlockedIds.Clear();
            Save();
            OnStatsChanged?.Invoke();
        }

        // ---------- 每猫统计 ----------

        public int GetCatGames(string catName) => GetOrCreate(catName).games;

        public int GetCatWins(string catName) => GetOrCreate(catName).wins;

        /// <summary>单只猫最多的对局数（老友记成就）。</summary>
        public int GetBestCatGames()
        {
            int best = 0;
            foreach (var cs in catStats) best = Mathf.Max(best, cs.games);
            return best;
        }

        private CatStat GetOrCreate(string catName)
        {
            var cs = catStats.Find(c => c.catName == catName);
            if (cs == null)
            {
                cs = new CatStat { catName = catName };
                catStats.Add(cs);
            }
            return cs;
        }

        // ---------- 持久化 ----------

        private void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(SavePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var data = new SaveData
                {
                    totalGames = TotalGames,
                    totalWins = TotalWins,
                    currentStreak = CurrentStreak,
                    maxStreak = MaxStreak,
                    currentLoseStreak = CurrentLoseStreak,
                    coinEarnedTotal = CoinEarnedTotal,
                    catStats = catStats,
                    unlockedIds = new List<string>(unlockedIds)
                };
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerStats] Save failed: {e.Message}");
            }
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return;
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                if (data == null) return;
                TotalGames = data.totalGames;
                TotalWins = data.totalWins;
                CurrentStreak = data.currentStreak;
                MaxStreak = data.maxStreak;
                CurrentLoseStreak = data.currentLoseStreak;
                CoinEarnedTotal = data.coinEarnedTotal;
                catStats.Clear();
                if (data.catStats != null) catStats.AddRange(data.catStats);
                unlockedIds.Clear();
                if (data.unlockedIds != null) foreach (var id in data.unlockedIds) unlockedIds.Add(id);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerStats] Load failed: {e.Message}");
            }
        }
    }
}
