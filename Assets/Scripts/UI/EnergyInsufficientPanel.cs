using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Wuziqi.Game;

namespace Wuziqi.UI
{
    /// <summary>体力不足提示弹窗：显示恢复倒计时 + 看广告恢复按钮。</summary>
    public class EnergyInsufficientPanel : MonoBehaviour
    {
        [Header("UI 引用")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private TMP_Text tipText;
        [SerializeField] private Button watchAdButton;
        [SerializeField] private Button closeButton;

        private float remainingSeconds;
        private bool isCounting;

        private void OnEnable()
        {
            if (watchAdButton) watchAdButton.onClick.AddListener(OnWatchAd);
            if (closeButton) closeButton.onClick.AddListener(Close);

            if (titleText) titleText.text = "体力不足";
            if (tipText) tipText.text = "看广告恢复1点体力，或等待自然恢复";

            // 检查是否还能看广告
            if (watchAdButton != null)
                watchAdButton.gameObject.SetActive(EconomyManager.Instance != null && EconomyManager.Instance.CanWatchEnergyAd);

            StartCountdown();
        }

        private void OnDisable()
        {
            if (watchAdButton) watchAdButton.onClick.RemoveListener(OnWatchAd);
            if (closeButton) closeButton.onClick.RemoveListener(Close);
            isCounting = false;
        }

        private void Update()
        {
            if (!isCounting) return;

            remainingSeconds -= Time.deltaTime;
            if (remainingSeconds <= 0f)
            {
                remainingSeconds = 0f;
                isCounting = false;
                UpdateCountdownDisplay();

                // 恢复了1点体力，自动关闭弹窗
                Close();
                return;
            }
            UpdateCountdownDisplay();
        }

        public void Show(float waitSeconds)
        {
            remainingSeconds = waitSeconds;
            isCounting = true;
            UpdateCountdownDisplay();
            gameObject.SetActive(true);
        }

        private void StartCountdown()
        {
            if (EconomyManager.Instance != null)
                remainingSeconds = EconomyManager.Instance.GetNextRecoverySeconds();
            isCounting = remainingSeconds > 0f;
            UpdateCountdownDisplay();
        }

        private void UpdateCountdownDisplay()
        {
            if (countdownText == null) return;
            int minutes = Mathf.FloorToInt(remainingSeconds / 60f);
            int seconds = Mathf.FloorToInt(remainingSeconds % 60f);
            countdownText.text = $"恢复倒计时 {minutes:00}:{seconds:00}";
        }

        private void OnWatchAd()
        {
            if (AdManager.Instance == null) return;

            AdManager.Instance.ShowRewarded("energy", success =>
            {
                if (success && EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.GrantEnergyAdReward();
                    Debug.Log("[EnergyInsufficientPanel] 看广告恢复1点体力");
                }
                Close();
            });
        }

        private void Close()
        {
            isCounting = false;
            var tbc = FindAnyObjectByType<TopBarController>();
            if (tbc != null) tbc.CloseAllPanels();
            else gameObject.SetActive(false);
        }
    }
}
