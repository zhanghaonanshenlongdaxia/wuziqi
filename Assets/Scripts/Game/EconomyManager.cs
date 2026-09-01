using UnityEngine;

namespace Wuziqi.Game
{
    /// <summary>仙喵币管理（PlayerPrefs 持久化）。体力已移除，玩家可无限对局。</summary>
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        private const string K_Coins = "Wuziqi.Coins";

        public int Coins { get; private set; }

        public event System.Action OnChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            Coins = PlayerPrefs.GetInt(K_Coins, 0);
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
    }
}
