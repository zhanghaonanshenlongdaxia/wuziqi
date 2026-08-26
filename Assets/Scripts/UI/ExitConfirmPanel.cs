using UnityEngine;
using UnityEngine.UI;

namespace Wuziqi.UI
{
    /// <summary>退出二次确认弹窗。</summary>
    public class ExitConfirmPanel : MonoBehaviour
    {
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button confirmButton;

        private void Start()
        {
            if (cancelButton) cancelButton.onClick.AddListener(Close);
            if (confirmButton) confirmButton.onClick.AddListener(Quit);
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Close()
        {
            var tbc = FindAnyObjectByType<TopBarController>();
            if (tbc != null) tbc.CloseAllPanels();
            else gameObject.SetActive(false);
        }
    }
}
