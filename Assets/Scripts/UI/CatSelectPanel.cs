using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Wuziqi.Game;

namespace Wuziqi.UI
{
    /// <summary>猫猫选择弹窗：用 CatItem 模板实例化猫猫格位，选择/解锁/切换。</summary>
    public class CatSelectPanel : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private Transform gridRoot;
        [SerializeField] private CatItem catItemPrefab;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descText;
        [SerializeField] private TMP_Text difficultyText;
        [SerializeField] private TMP_Text unlockHintText;
        [SerializeField] private Button unlockAdButton;
        [SerializeField] private Button unlockCoinsButton;

        private int previewIndex;
        private CatProfile[] cats;

        private void Start()
        {
            if (closeButton) closeButton.onClick.AddListener(Close);
            if (confirmButton) confirmButton.onClick.AddListener(Confirm);
            if (unlockAdButton) unlockAdButton.onClick.AddListener(TryUnlockByAd);
            if (unlockCoinsButton) unlockCoinsButton.onClick.AddListener(TryUnlockByCoins);
        }

        private void OnEnable() => BuildGrid();

        private bool building; // 防止同帧重复构建（Destroy 延迟销毁导致旧项残留）

        private void BuildGrid()
        {
            if (CatManager.Instance == null || gridRoot == null || building) return;
            building = true;

            cats = new CatProfile[CatManager.Instance.CatCount];
            for (int i = 0; i < cats.Length; i++)
                cats[i] = CatManager.Instance.GetCat(i);

            // clear old items（DestroyImmediate 保证清理立即生效）
            for (int i = gridRoot.childCount - 1; i >= 0; i--)
                DestroyImmediate(gridRoot.GetChild(i).gameObject);

            for (int i = 0; i < cats.Length; i++)
            {
                int idx = i;
                CatItem item;
                if (catItemPrefab != null)
                    item = Instantiate(catItemPrefab, gridRoot);
                else
                    item = new GameObject("Cat", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CatItem)).GetComponent<CatItem>();

                var c = cats[i];
                item.SetPortrait(c.portrait);
                item.SetName(c.catName);
                item.SetLocked(!CatManager.Instance.IsUnlocked(idx));
                item.Button.onClick.AddListener(() => Preview(idx));
            }
            Preview(CatManager.Instance.SelectedIndex);
            building = false;
        }

        private void Preview(int index)
        {
            if (cats == null || index < 0 || index >= cats.Length) return;
            previewIndex = index;
            var c = cats[index];
            bool unlocked = CatManager.Instance.IsUnlocked(index);
            bool isCurrent = CatManager.Instance != null && CatManager.Instance.SelectedIndex == index;

            if (nameText) nameText.text = c.catName;
            if (descText) descText.text = c.description;
            if (difficultyText) difficultyText.text = $"难度 {new string('★', c.difficulty)}{new string('☆', 5 - c.difficulty)}";
            if (unlockHintText) unlockHintText.text = !unlocked ? GetUnlockHint(c) : (isCurrent ? "出战中" : "已解锁");
            if (unlockAdButton) unlockAdButton.gameObject.SetActive(!unlocked && c.unlockType == CatProfile.UnlockType.Ad);
            if (unlockCoinsButton) unlockCoinsButton.gameObject.SetActive(!unlocked && c.unlockType == CatProfile.UnlockType.Coins);
            if (confirmButton)
            {
                confirmButton.interactable = unlocked && !isCurrent;
                var label = confirmButton.GetComponentInChildren<TMPro.TMP_Text>();
                if (label != null) label.text = isCurrent ? "出战中" : "出 战";
            }
        }

        private string GetUnlockHint(CatProfile c)
        {
            return c.unlockType switch
            {
                CatProfile.UnlockType.Ad => "看广告解锁",
                CatProfile.UnlockType.Coins => $"{c.coinCost} 仙喵币解锁",
                _ => "免费",
            };
        }

        private void Confirm()
        {
            if (CatManager.Instance != null)
                CatManager.Instance.SelectCat(previewIndex);
            Close();
        }

        private void TryUnlockByAd()
        {
            if (AdManager.Instance != null)
                AdManager.Instance.ShowRewarded("unlock_cat_" + previewIndex, success =>
                {
                    if (success && CatManager.Instance != null)
                    {
                        CatManager.Instance.UnlockByAd(previewIndex);
                        BuildGrid();
                        Preview(previewIndex);
                    }
                });
        }

        private void TryUnlockByCoins()
        {
            if (CatManager.Instance != null && CatManager.Instance.UnlockByCoins(previewIndex))
            {
                BuildGrid();
                Preview(previewIndex);
            }
            else
            {
                if (unlockHintText) unlockHintText.text = "仙喵币不足！";
            }
        }

        private void Close()
        {
            var tbc = FindAnyObjectByType<TopBarController>();
            if (tbc != null) tbc.CloseAllPanels();
            else gameObject.SetActive(false);
        }
    }
}
