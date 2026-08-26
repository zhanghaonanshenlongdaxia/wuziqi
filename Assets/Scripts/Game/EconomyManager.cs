using UnityEngine;

namespace Wuziqi.Game
{
    /// <summary>体力与仙喵币管理（PlayerPrefs 持久化）。</summary>
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        private const string K_Energy = "Wuziqi.Energy";
        private const string K_EnergyMax = "Wuziqi.EnergyMax";
        private const string K_Coins = "Wuziqi.Coins";
        private const string K_RewardDate = "Wuziqi.RewardDate";
        private const string K_EnergyAdCount = "Wuziqi.EnergyAdCount";
        private const string K_CoinsAdCount = "Wuziqi.CoinsAdCount";

        public const int DefaultEnergyMax = 5;
        public const int MaxAdRewardsPerDay = 3;
        public const int EnergyPerAd = 1;
        public const int CoinsPerAd = 20;

        public int Energy { get; private set; }
        public int EnergyMax { get; private set; }
        public int Coins { get; private set; }
        public int EnergyAdCount { get; private set; }
        public int CoinsAdCount { get; private set; }

        public event System.Action OnChanged;

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
        }

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
