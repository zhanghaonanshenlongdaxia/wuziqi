using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Wuziqi.Game;

namespace Wuziqi.UI
{
    /// <summary>右上角按钮组 + 左上角仙喵币显示控制器。</summary>
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

        [Header("弹窗引用")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject catSelectPanel;
        [SerializeField] private ConfirmDialog confirmDialogPrefab;
        [SerializeField] private GameObject songListPanel;
        [SerializeField] private GameObject dimMask;  // 全屏半透明遮罩

        private ConfirmDialog activeConfirmDialog;

        private void Start()
        {
            settingsButton.onClick.AddListener(OpenSettings);
            catSelectButton.onClick.AddListener(OpenCatSelect);
            exitButton.onClick.AddListener(OpenExitConfirm);
            if (songListButton) songListButton.onClick.AddListener(OpenSongList);

            // 体力系统已移除，显示无限
            if (energyText) energyText.text = "体力 ∞";
            if (energyAddButton) energyAddButton.gameObject.SetActive(false);

            if (settingsPanel) settingsPanel.SetActive(false);
            if (catSelectPanel) catSelectPanel.SetActive(false);
            if (songListPanel) songListPanel.SetActive(false);
            if (dimMask) dimMask.SetActive(false);

            UpdateDisplay();
            if (Wuziqi.Game.EconomyManager.Instance != null)
                Wuziqi.Game.EconomyManager.Instance.OnChanged += UpdateDisplay;
        }

        private void OnDestroy()
        {
            if (Wuziqi.Game.EconomyManager.Instance != null)
                Wuziqi.Game.EconomyManager.Instance.OnChanged -= UpdateDisplay;
        }

        public void UpdateDisplay()
        {
            var eco = Wuziqi.Game.EconomyManager.Instance;
            if (eco == null) return;
            if (coinsText) coinsText.text = $"仙喵币 {eco.Coins}";
        }

        private void OpenSettings()
        {
            if (dimMask) dimMask.SetActive(true);
            if (settingsPanel) settingsPanel.SetActive(true);
            GameManager.Instance?.PauseGame();
        }

        private void OpenCatSelect()
        {
            if (dimMask) dimMask.SetActive(true);
            if (catSelectPanel) catSelectPanel.SetActive(true);
            GameManager.Instance?.PauseGame();
        }

        private void OpenExitConfirm()
        {
            if (!confirmDialogPrefab) return;
            if (dimMask) dimMask.SetActive(true);
            GameManager.Instance?.PauseGame();

            activeConfirmDialog = Instantiate(confirmDialogPrefab, transform.root);
            activeConfirmDialog.Show(
                "确定退出游戏吗？",
                onConfirm: () =>
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                },
                onCancel: () => CloseAllPanels(),
                title: "退出游戏",
                confirmText: "退出",
                cancelText: "取消"
            );
        }

        private void OpenSongList()
        {
            if (dimMask) dimMask.SetActive(true);
            if (songListPanel) songListPanel.SetActive(true);
            GameManager.Instance?.PauseGame();
        }

        /// <summary>关闭所有弹窗（由各弹窗的关闭按钮调用）。</summary>
        public void CloseAllPanels()
        {
            if (settingsPanel) settingsPanel.SetActive(false);
            if (catSelectPanel) catSelectPanel.SetActive(false);
            if (activeConfirmDialog) { Destroy(activeConfirmDialog.gameObject); activeConfirmDialog = null; }
            if (songListPanel) songListPanel.SetActive(false);
            if (dimMask) dimMask.SetActive(false);
            GameManager.Instance?.ResumeGame();
        }
    }
}
