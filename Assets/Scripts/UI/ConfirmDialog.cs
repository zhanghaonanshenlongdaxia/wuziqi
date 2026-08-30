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
        /// <param name="message">提示文本</param>
        /// <param name="onConfirm">确认回调</param>
        /// <param name="onCancel">取消回调（可选）</param>
        /// <param name="title">标题（可选，默认"提示"）</param>
        /// <param name="confirmText">确认按钮文本（可选，默认"确认"）</param>
        /// <param name="cancelText">取消按钮文本（可选，默认"取消"）</param>
        public void Show(string message, Action onConfirm, Action onCancel = null,
                         string title = null, string confirmText = null, string cancelText = null)
        {
            this.onConfirm = onConfirm;
            this.onCancel = onCancel;

            if (titleText) titleText.text = title ?? "提示";
            if (messageText) messageText.text = message;
            if (confirmLabel) confirmLabel.text = confirmText ?? "确认";
            if (cancelLabel) cancelLabel.text = cancelText ?? "取消";

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
