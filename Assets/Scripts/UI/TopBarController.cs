using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Wuziqi.UI
{
    /// <summary>右上角按钮组 + 左上角体力/仙喵币显示控制器。</summary>
    public class TopBarController : MonoBehaviour
    {
        [Header("右上角按钮")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button catSelectButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button songListButton;

        [Header("左上角资源")]
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private Button energyAddButton;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private Button coinsAddButton;

        [Header("弹窗引用")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject catSelectPanel;
        [SerializeField] private GameObject exitConfirmPanel;
        [SerializeField] private GameObject rewardPanel;
        [SerializeField] private GameObject songListPanel;
        [SerializeField] private EnergyInsufficientPanel energyInsufficientPanel;
        [SerializeField] private GameObject dimMask;  // 全屏半透明遮罩

        private Coroutine energyFlashCoroutine;

        private void Start()
        {
            settingsButton.onClick.AddListener(OpenSettings);
            catSelectButton.onClick.AddListener(OpenCatSelect);
            exitButton.onClick.AddListener(OpenExitConfirm);
            if (songListButton) songListButton.onClick.AddListener(OpenSongList);
            energyAddButton.onClick.AddListener(OpenReward);
            coinsAddButton.onClick.AddListener(OpenReward);

            if (settingsPanel) settingsPanel.SetActive(false);
            if (catSelectPanel) catSelectPanel.SetActive(false);
            if (exitConfirmPanel) exitConfirmPanel.SetActive(false);
            if (rewardPanel) rewardPanel.SetActive(false);
            if (songListPanel) songListPanel.SetActive(false);
            if (dimMask) dimMask.SetActive(false);

            UpdateDisplay();
            if (Wuziqi.Game.EconomyManager.Instance != null)
            {
                Wuziqi.Game.EconomyManager.Instance.OnChanged += UpdateDisplay;
                Wuziqi.Game.EconomyManager.Instance.OnEnergyInsufficient += OnEnergyInsufficient;
            }
        }

        private void OnDestroy()
        {
            if (Wuziqi.Game.EconomyManager.Instance != null)
            {
                Wuziqi.Game.EconomyManager.Instance.OnChanged -= UpdateDisplay;
                Wuziqi.Game.EconomyManager.Instance.OnEnergyInsufficient -= OnEnergyInsufficient;
            }
        }

        public void UpdateDisplay()
        {
            var eco = Wuziqi.Game.EconomyManager.Instance;
            if (eco == null) return;
            if (energyText) energyText.text = $"体力 {eco.Energy}/{eco.EnergyMax}";
            if (coinsText) coinsText.text = $"仙喵币 {eco.Coins}";
        }

        /// <summary>体力不足时闪烁体力文字。</summary>
        private void OnEnergyInsufficient(float waitSeconds)
        {
            if (energyFlashCoroutine != null) StopCoroutine(energyFlashCoroutine);
            energyFlashCoroutine = StartCoroutine(FlashEnergyText());

            // 同时弹出体力不足面板
            if (energyInsufficientPanel != null)
                energyInsufficientPanel.Show(waitSeconds);
        }

        private IEnumerator FlashEnergyText()
        {
            if (energyText == null) yield break;

            Color originalColor = energyText.color;
            Color flashColor = Color.red;
            float flashDuration = 0.3f;
            int flashCount = 3;

            for (int i = 0; i < flashCount; i++)
            {
                energyText.color = flashColor;
                yield return new WaitForSeconds(flashDuration);
                energyText.color = originalColor;
                yield return new WaitForSeconds(flashDuration);
            }
            energyFlashCoroutine = null;
        }

        private void OpenSettings()
        {
            if (dimMask) dimMask.SetActive(true);
            if (settingsPanel) settingsPanel.SetActive(true);
        }

        private void OpenCatSelect()
        {
            if (dimMask) dimMask.SetActive(true);
            if (catSelectPanel) catSelectPanel.SetActive(true);
        }

        private void OpenExitConfirm()
        {
            if (dimMask) dimMask.SetActive(true);
            if (exitConfirmPanel) exitConfirmPanel.SetActive(true);
        }

        private void OpenReward()
        {
            if (dimMask) dimMask.SetActive(true);
            if (rewardPanel) rewardPanel.SetActive(true);
        }

        private void OpenSongList()
        {
            if (dimMask) dimMask.SetActive(true);
            if (songListPanel) songListPanel.SetActive(true);
        }

        /// <summary>关闭所有弹窗（由各弹窗的关闭按钮调用）。</summary>
        public void CloseAllPanels()
        {
            if (settingsPanel) settingsPanel.SetActive(false);
            if (catSelectPanel) catSelectPanel.SetActive(false);
            if (exitConfirmPanel) exitConfirmPanel.SetActive(false);
            if (rewardPanel) rewardPanel.SetActive(false);
            if (songListPanel) songListPanel.SetActive(false);
            if (energyInsufficientPanel != null) energyInsufficientPanel.gameObject.SetActive(false);
            if (dimMask) dimMask.SetActive(false);
        }
    }
}
