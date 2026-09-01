using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Wuziqi.UI
{
    /// <summary>通用二次确认弹窗：可配置标题、正文、按钮文本和回调。</summary>
    public class ConfirmDialog : MonoBehaviour
    {
        [Header("UI 引用")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text confirmLabel;
        [SerializeField] private TMP_Text cancelLabel;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private Action onConfirm;
        private Action onCancel;

        private void Awake()
        {
            if (confirmButton) confirmButton.onClick.AddListener(OnConfirmClicked);
            if (cancelButton) cancelButton.onClick.AddListener(OnCancelClicked);
            gameObject.SetActive(false);
        }

        /// <summary>显示确认弹窗。</summary>
        public void Show(string message, Action onConfirm, Action onCancel = null,
                         string title = null, string confirmText = null, string cancelText = null)
        {
            this.onConfirm = onConfirm;
            this.onCancel = onCancel;

            if (titleText)
            {
                bool hasTitle = !string.IsNullOrEmpty(title);
                titleText.gameObject.SetActive(hasTitle);
                if (hasTitle) titleText.text = title;
            }
            if (messageText) messageText.text = message;
            if (confirmLabel) confirmLabel.text = confirmText ?? "确认";

            // 没传onCancel时隐藏取消按钮（如问候弹窗）
            bool showCancel = onCancel != null || cancelText != null;
            if (cancelLabel) cancelLabel.text = cancelText ?? "取消";
            if (cancelButton) cancelButton.gameObject.SetActive(showCancel);

            gameObject.SetActive(true);
        }

        /// <summary>隐藏弹窗。</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            onConfirm = null;
            onCancel = null;
        }

        private void OnConfirmClicked()
        {
            var cb = onConfirm;
            Hide();
            cb?.Invoke();
        }

        private void OnCancelClicked()
        {
            var cb = onCancel;
            Hide();
            cb?.Invoke();
        }
    }
}
