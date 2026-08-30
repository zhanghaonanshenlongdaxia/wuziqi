using System.Collections;
using UnityEngine;

namespace Wuziqi.Game
{
    /// <summary>体力与仙喵币管理（PlayerPrefs 持久化）。
    /// 体力每分钟恢复1点，离线也会计入。</summary>
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        private const string K_Energy = "Wuziqi.Energy";
        private const string K_EnergyMax = "Wuziqi.EnergyMax";
        private const string K_Coins = "Wuziqi.Coins";
        private const string K_RewardDate = "Wuziqi.RewardDate";
        private const string K_EnergyAdCount = "Wuziqi.EnergyAdCount";
        private const string K_CoinsAdCount = "Wuziqi.CoinsAdCount";
        private const string K_LastRecoveryTime = "Wuziqi.LastRecoveryTime";

        public const int DefaultEnergyMax = 5;
        public const int MaxAdRewardsPerDay = 3;
        public const int EnergyPerAd = 1;
        public const int CoinsPerAd = 20;
        public const int EnergyCostPerGame = 1;
        public const float RecoveryIntervalSeconds = 300f; // 每5分钟恢复1点

        public int Energy { get; private set; }
        public int EnergyMax { get; private set; }
        public int Coins { get; private set; }
        public int EnergyAdCount { get; private set; }
        public int CoinsAdCount { get; private set; }

        /// <summary>上次体力恢复的时间戳（Unix秒）。</summary>
        private double lastRecoveryTime;

        public event System.Action OnChanged;

        /// <summary>体力不足时触发（参数为距下次恢复的秒数）。</summary>
        public event System.Action<float> OnEnergyInsufficient;

        private Coroutine recoveryCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            EnergyMax = PlayerPrefs.GetInt(K_EnergyMax, DefaultEnergyMax);
            Energy = PlayerPrefs.GetInt(K_Energy, EnergyMax);
            Coins = PlayerPrefs.GetInt(K_Coins, 0);
            ResetDailyIfNeeded();
            EnergyAdCount = PlayerPrefs.GetInt(K_EnergyAdCount, 0);
            CoinsAdCount = PlayerPrefs.GetInt(K_CoinsAdCount, 0);

            // 离线恢复：计算上次登录到现在的恢复量
            lastRecoveryTime = PlayerPrefs.GetFloat(K_LastRecoveryTime, 0);
            RecoverOffline();
            SaveLastRecoveryTime();

            // 启动定时恢复协程
            StartRecoveryCoroutine();
        }

        private void OnDestroy()
        {
            if (recoveryCoroutine != null) StopCoroutine(recoveryCoroutine);
        }

        // ---------- 体力消耗 ----------

        /// <summary>尝试开始一局游戏，扣1点体力。体力不足返回false。</summary>
        public bool TryStartGame()
        {
            if (Energy < EnergyCostPerGame)
            {
                float waitSeconds = GetNextRecoverySeconds();
                OnEnergyInsufficient?.Invoke(waitSeconds);
                return false;
            }
            SpendEnergy(EnergyCostPerGame);
            return true;
        }

        /// <summary>获取距下次体力恢复的秒数。</summary>
        public float GetNextRecoverySeconds()
        {
            if (Energy >= EnergyMax) return 0f;
            double now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            double elapsed = now - lastRecoveryTime;
            if (elapsed >= RecoveryIntervalSeconds) return 0f;
            return (float)(RecoveryIntervalSeconds - elapsed);
        }

        // ---------- 体力操作 ----------

        public bool SpendEnergy(int amount)
        {
            if (Energy < amount) return false;
            Energy -= amount;
            PlayerPrefs.SetInt(K_Energy, Energy);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
            return true;
        }

        public void AddEnergy(int amount)
        {
            Energy = Mathf.Min(Energy + amount, EnergyMax);
            PlayerPrefs.SetInt(K_Energy, Energy);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }

        // ---------- 金币操作 ----------

        public void AddCoins(int amount)
        {
            Coins += amount;
            PlayerPrefs.SetInt(K_Coins, Coins);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }

        public bool SpendCoins(int amount)
        {
            if (Coins < amount) return false;
            Coins -= amount;
            PlayerPrefs.SetInt(K_Coins, Coins);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
            return true;
        }

        // ---------- 广告奖励 ----------

        public bool CanWatchEnergyAd => EnergyAdCount < MaxAdRewardsPerDay;
        public bool CanWatchCoinsAd => CoinsAdCount < MaxAdRewardsPerDay;

        public void GrantEnergyAdReward()
        {
            AddEnergy(EnergyPerAd);
            EnergyAdCount++;
            PlayerPrefs.SetInt(K_EnergyAdCount, EnergyAdCount);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }

        public void GrantCoinsAdReward()
        {
            AddCoins(CoinsPerAd);
            CoinsAdCount++;
            PlayerPrefs.SetInt(K_CoinsAdCount, CoinsAdCount);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }

        // ---------- 时间恢复 ----------

        /// <summary>离线期间的体力恢复。</summary>
        private void RecoverOffline()
        {
            if (lastRecoveryTime <= 0) return; // 首次运行，不恢复
            if (Energy >= EnergyMax) return;

            double now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            double elapsed = now - lastRecoveryTime;
            int recoverCount = Mathf.FloorToInt((float)(elapsed / RecoveryIntervalSeconds));
            if (recoverCount > 0)
            {
                AddEnergy(recoverCount);
                Debug.Log($"[EconomyManager] 离线恢复 {recoverCount} 点体力");
            }
        }

        private void StartRecoveryCoroutine()
        {
            if (recoveryCoroutine != null) StopCoroutine(recoveryCoroutine);
            recoveryCoroutine = StartCoroutine(RecoveryLoop());
        }

        private IEnumerator RecoveryLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(RecoveryIntervalSeconds);

                if (Energy < EnergyMax)
                {
                    AddEnergy(1);
                    SaveLastRecoveryTime();
                    Debug.Log($"[EconomyManager] 体力恢复1点，当前 {Energy}/{EnergyMax}");
                }
            }
        }

        private void SaveLastRecoveryTime()
        {
            lastRecoveryTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            PlayerPrefs.SetFloat(K_LastRecoveryTime, (float)lastRecoveryTime);
            PlayerPrefs.Save();
        }

        // ---------- 每日重置 ----------

        private void ResetDailyIfNeeded()
        {
            string today = System.DateTime.Now.ToString("yyyy-MM-dd");
            string saved = PlayerPrefs.GetString(K_RewardDate, "");
            if (saved != today)
            {
                PlayerPrefs.SetString(K_RewardDate, today);
                PlayerPrefs.SetInt(K_EnergyAdCount, 0);
                PlayerPrefs.SetInt(K_CoinsAdCount, 0);
                PlayerPrefs.Save();
            }
        }
    }
}
