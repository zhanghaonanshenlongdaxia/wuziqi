using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Wuziqi.Game;

namespace Wuziqi.UI
{
    /// <summary>看广告获取体力/仙喵币弹窗。</summary>
    public class RewardPanel : MonoBehaviour
    {
        [SerializeField] private Button energyAdButton;
        [SerializeField] private Button coinsAdButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text energyCountText;
        [SerializeField] private TMP_Text coinsCountText;

        private void Start()
        {
            if (energyAdButton) energyAdButton.onClick.AddListener(WatchEnergyAd);
            if (coinsAdButton) coinsAdButton.onClick.AddListener(WatchCoinsAd);
            if (closeButton) closeButton.onClick.AddListener(Close);
        }

        private void OnEnable() => UpdateDisplay();

        private void UpdateDisplay()
        {
            var eco = EconomyManager.Instance;
            if (eco == null) return;
            if (energyCountText) energyCountText.text = $"今日剩余 {EconomyManager.MaxAdRewardsPerDay - eco.EnergyAdCount}/{EconomyManager.MaxAdRewardsPerDay}";
            if (coinsCountText) coinsCountText.text = $"今日剩余 {EconomyManager.MaxAdRewardsPerDay - eco.CoinsAdCount}/{EconomyManager.MaxAdRewardsPerDay}";
            if (energyAdButton) energyAdButton.interactable = eco.CanWatchEnergyAd;
            if (coinsAdButton) coinsAdButton.interactable = eco.CanWatchCoinsAd;
        }

        private void WatchEnergyAd()
        {
            if (AdManager.Instance != null)
                AdManager.Instance.ShowRewarded("energy", success =>
                {
                    if (success && EconomyManager.Instance != null)
                        EconomyManager.Instance.GrantEnergyAdReward();
                    UpdateDisplay();
                });
        }

        private void WatchCoinsAd()
        {
            if (AdManager.Instance != null)
                AdManager.Instance.ShowRewarded("coins", success =>
                {
                    if (success && EconomyManager.Instance != null)
                        EconomyManager.Instance.GrantCoinsAdReward();
                    UpdateDisplay();
                });
        }

        private void Close()
        {
            var tbc = FindAnyObjectByType<TopBarController>();
            if (tbc != null) tbc.CloseAllPanels();
            else gameObject.SetActive(false);
        }
    }
}
