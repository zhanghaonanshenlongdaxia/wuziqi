using System.Collections.Generic;
using UnityEngine;

namespace Wuziqi.Game
{
    /// <summary>管理猫猫解锁状态、当前选中猫、切换逻辑。</summary>
    public class CatManager : MonoBehaviour
    {
        public static CatManager Instance { get; private set; }

        [Header("猫猫数据列表")]
        [SerializeField] private CatProfile[] cats;

        private const string K_Selected = "Wuziqi.CatSelected";
        private const string K_UnlockPrefix = "Wuziqi.CatUnlock.";

        public int SelectedIndex { get; private set; }
        public CatProfile Selected => cats != null && SelectedIndex < cats.Length ? cats[SelectedIndex] : null;
        public int CatCount => cats != null ? cats.Length : 0;

        public event System.Action<int> OnCatChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            SelectedIndex = PlayerPrefs.GetInt(K_Selected, 0);
            if (cats == null || cats.Length == 0)
                Debug.LogError("[CatManager] No cats assigned!");
        }

        public CatProfile GetCat(int index)
        {
            if (cats == null || index < 0 || index >= cats.Length) return null;
            return cats[index];
        }

        public bool IsUnlocked(int index)
        {
            if (cats == null || index < 0 || index >= cats.Length) return false;
            var c = cats[index];
            if (c.unlockType == CatProfile.UnlockType.Free) return true;
            return PlayerPrefs.GetInt(K_UnlockPrefix + index, 0) == 1;
        }

        public void SelectCat(int index)
        {
            if (!IsUnlocked(index))
            {
                Debug.LogWarning($"[CatManager] Cat {index} not unlocked");
                return;
            }
            SelectedIndex = index;
            PlayerPrefs.SetInt(K_Selected, index);
            PlayerPrefs.Save();
            OnCatChanged?.Invoke(index);
        }

        /// <summary>通过仙喵币解锁。</summary>
        public bool UnlockByCoins(int index)
        {
            if (index < 0 || index >= cats.Length) return false;
            var c = cats[index];
            if (EconomyManager.Instance == null) return false;
            if (!EconomyManager.Instance.SpendCoins(c.coinCost)) return false;
            PlayerPrefs.SetInt(K_UnlockPrefix + index, 1);
            PlayerPrefs.Save();
            Debug.Log($"[CatManager] Cat {index} ({c.catName}) unlocked via {c.coinCost} coins");
            return true;
        }
    }
}
