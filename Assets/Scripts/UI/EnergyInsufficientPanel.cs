using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Wuziqi.UI
{
    /// <summary>体力不足提示弹窗（体力系统已移除，保留文件兼容场景引用）。</summary>
    public class EnergyInsufficientPanel : MonoBehaviour
    {
        [SerializeField] private Button closeButton;

        private void OnEnable()
        {
            if (closeButton) closeButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            if (closeButton) closeButton.onClick.RemoveListener(Close);
        }

        public void Show(float waitSeconds)
        {
            // 体力系统已移除，不再显示
            gameObject.SetActive(false);
        }

        private void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
