using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Wuziqi.UI
{
    /// <summary>资源提示面板（仅显示仙喵币获取说明）。</summary>
    public class RewardPanel : MonoBehaviour
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text messageText;

        private void Start()
        {
            if (closeButton) closeButton.onClick.AddListener(Close);
        }

        private void OnEnable()
        {
            if (messageText) messageText.text = "胜利可获得仙喵币奖励\n用于解锁猫猫和歌曲";
        }

        private void Close()
        {
            var tbc = FindAnyObjectByType<TopBarController>();
            if (tbc != null) tbc.CloseAllPanels();
            else gameObject.SetActive(false);
        }
    }
}
