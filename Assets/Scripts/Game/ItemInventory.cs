using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wuziqi.Game
{
    /// <summary>道具库存（提示/双倍卡）：PlayerPrefs 持久化，首次登录送体验装。挂 GameManagers 节点。</summary>
    public class ItemInventory : MonoBehaviour
    {
        public static ItemInventory Instance { get; private set; }

        public const int HINT_COST = 15;
        public const int DOUBLE_COST = 20;

        public int HintCount { get; private set; }
        public int DoubleCount { get; private set; }

        /// <summary>本局双倍奖励是否已激活（GameEnded 时由结算方消耗）。</summary>
        public bool DoubleActive { get; private set; }

        public event Action OnChanged;

        private const string K_Hint = "Wuziqi.Item.Hint";
        private const string K_Double = "Wuziqi.Item.Double";
        private const string K_DoubleActive = "Wuziqi.Item.DoubleActive";
        private const string K_Gifted = "Wuziqi.Item.Gifted";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            HintCount = PlayerPrefs.GetInt(K_Hint, 0);
            DoubleCount = PlayerPrefs.GetInt(K_Double, 0);
            DoubleActive = PlayerPrefs.GetInt(K_DoubleActive, 0) == 1;

            // 首次登录赠送体验装
            if (PlayerPrefs.GetInt(K_Gifted, 0) == 0)
            {
                HintCount += 2;
                DoubleCount += 2;
                PlayerPrefs.SetInt(K_Gifted, 1);
                Save();
            }

            // 启动即清残留的双倍激活状态（"本局"不跨启动，防白嫖一次双倍）
            if (DoubleActive)
            {
                DoubleActive = false;
                PlayerPrefs.SetInt(K_DoubleActive, 0);
                Save();
            }
        }

        public bool TryUseHint()
        {
            if (HintCount <= 0) return false;
            HintCount--;
            PlayerPrefs.SetInt(K_Hint, HintCount);
            Save();
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>激活本局双倍（库存-1；对局结束时由结算方 ConsumeDouble）。</summary>
        public bool TryUseDouble()
        {
            if (DoubleCount <= 0 || DoubleActive) return false;
            DoubleCount--;
            DoubleActive = true;
            PlayerPrefs.SetInt(K_Double, DoubleCount);
            PlayerPrefs.SetInt(K_DoubleActive, 1);
            Save();
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>结算时消耗双倍状态。</summary>
        public void ConsumeDouble()
        {
            if (!DoubleActive) return;
            DoubleActive = false;
            PlayerPrefs.SetInt(K_DoubleActive, 0);
            Save();
            OnChanged?.Invoke();
        }

        public void AddHint()
        {
            HintCount++;
            PlayerPrefs.SetInt(K_Hint, HintCount);
            Save();
            OnChanged?.Invoke();
        }

        public void AddDouble()
        {
            DoubleCount++;
            PlayerPrefs.SetInt(K_Double, DoubleCount);
            Save();
            OnChanged?.Invoke();
        }

        private void Save()
        {
            PlayerPrefs.SetInt(K_Hint, HintCount);
            PlayerPrefs.SetInt(K_Double, DoubleCount);
            PlayerPrefs.SetInt(K_DoubleActive, DoubleActive ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
