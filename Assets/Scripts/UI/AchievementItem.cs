using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Wuziqi.UI
{
    /// <summary>成就条目模板：数据填充由 AchievementPanel 调用（同 SongItem/CatItem 惯例）。</summary>
    public class AchievementItem : MonoBehaviour
    {
        [Header("文本槽")]
        [SerializeField] private TMP_Text nameText;      // 成就名称
        [SerializeField] private TMP_Text descText;      // 条件描述
        [SerializeField] private TMP_Text rewardText;    // 奖励币数（配 RewardIcon）
        [SerializeField] private TMP_Text progressText;  // "12/20"
        [SerializeField] private Image achIcon;          // 成就图标（水墨小景）

        [Header("进度条")]
        [SerializeField] private Image progressFill;     // 宽度按比例拉伸

        [Header("状态")]
        [SerializeField] private GameObject doneMark;    // 已达成角标（达成时显示）

        public void SetName(string v) { if (nameText) nameText.text = v; }

        public void SetDesc(string v) { if (descText) descText.text = v; }

        public void SetReward(int coins) { if (rewardText) rewardText.text = $"+{coins}"; }

        public void SetIcon(Sprite icon)
        {
            if (achIcon == null) return;
            achIcon.gameObject.SetActive(icon != null);
            if (icon != null) achIcon.sprite = icon;
        }

        public void SetProgress(int cur, int max)
        {
            if (progressText) progressText.text = $"{cur}/{max}";
            if (progressFill)
            {
                var rt = (RectTransform)progressFill.transform;
                float ratio = max > 0 ? Mathf.Clamp01((float)cur / max) : 0f;
                rt.anchorMax = new Vector2(ratio, rt.anchorMax.y);
            }
        }

        public void SetUnlocked(bool unlocked)
        {
            if (doneMark) doneMark.SetActive(unlocked);
            if (progressText) progressText.gameObject.SetActive(!unlocked);
            if (progressFill) progressFill.transform.parent.gameObject.SetActive(!unlocked);
        }
    }
}
