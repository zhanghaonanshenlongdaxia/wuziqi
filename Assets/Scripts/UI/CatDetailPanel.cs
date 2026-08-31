using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Wuziqi.Game;

namespace Wuziqi.UI
{
    /// <summary>猫猫详情弹窗：展示猫猫详细信息，从 CatSelectPanel 的详情按钮打开。</summary>
    public class CatDetailPanel : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text difficultyStars;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private TMP_Text aiInfoText;
        [SerializeField] private TMP_Text unlockStatus;
        [SerializeField] private TMP_Text descText;
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            if (closeButton) closeButton.onClick.AddListener(Close);
        }

        /// <summary>打开详情面板，显示指定猫猫的信息。</summary>
        public void Open(CatProfile cat, int catIndex)
        {
            if (cat == null) return;

            if (nameText) nameText.text = cat.catName;
            if (descText) descText.text = cat.description;
            if (portraitImage) portraitImage.sprite = cat.portrait;

            // 难度星级
            if (difficultyStars)
                difficultyStars.text = $"难度 {new string('★', cat.difficulty)}{new string('☆', 5 - cat.difficulty)}";

            // 奖励和挑战费用
            if (rewardText)
            {
                string reward = $"胜利 +{cat.winReward} 币";
                string cost = cat.challengeCost > 0 ? $"  |  挑战 -{cat.challengeCost} 币" : "";
                rewardText.text = reward + cost;
            }

            // AI 信息
            if (aiInfoText)
                aiInfoText.text = $"搜索深度 {cat.aiSearchDepth} 层  |  强度 {cat.aiScoreMultiplier:F1}x";

            // 解锁状态
            if (unlockStatus)
            {
                bool unlocked = CatManager.Instance != null && CatManager.Instance.IsUnlocked(catIndex);
                bool isCurrent = CatManager.Instance != null && CatManager.Instance.SelectedIndex == catIndex;
                if (isCurrent)
                    unlockStatus.text = "出战中";
                else if (unlocked)
                    unlockStatus.text = "已解锁";
                else
                    unlockStatus.text = cat.unlockType switch
                    {
                        CatProfile.UnlockType.Ad => "未解锁（看广告）",
                        CatProfile.UnlockType.Coins => $"未解锁（{cat.coinCost} 仙喵币）",
                        _ => "未解锁",
                    };
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
