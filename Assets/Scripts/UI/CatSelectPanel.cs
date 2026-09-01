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

        [Header("猫猫详情")]
        [SerializeField] private Button detailButton;
        [SerializeField] private CatDetailPanel detailPanel;

        [Header("切换确认弹窗")]
        [SerializeField] private ConfirmDialog confirmDialogPrefab;

        private int previewIndex;
        private CatProfile[] cats;
        private ConfirmDialog activeConfirmDialog;

        private void Start()
        {
            if (closeButton) closeButton.onClick.AddListener(Close);
            if (confirmButton) confirmButton.onClick.AddListener(Confirm);
            if (unlockAdButton) unlockAdButton.onClick.AddListener(TryUnlockByAd);
            if (unlockCoinsButton) unlockCoinsButton.onClick.AddListener(TryUnlockByCoins);
            if (detailButton) detailButton.onClick.AddListener(ShowDetail);
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
                if (label != null)
                {
                    if (isCurrent) label.text = "出战中";
                    else if (!unlocked) label.text = "出 战";
                    else label.text = c.challengeCost > 0 ? $"挑战（{c.challengeCost} 币）" : "出 战";
                }
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

        private void ShowDetail()
        {
            if (detailPanel != null && cats != null && previewIndex >= 0 && previewIndex < cats.Length)
                detailPanel.Open(cats[previewIndex], previewIndex);
        }

        private void Confirm()
        {
            if (confirmDialogPrefab)
            {
                activeConfirmDialog = Instantiate(confirmDialogPrefab, transform.root);
                var gm = GameManager.Instance;
                bool inGame = gm != null && !gm.IsGameOver && gm.Board.MoveCount > 0;
                activeConfirmDialog.Show(
                    inGame ? "切换猫猫需要重新开始，确认吗？" : "确认出战这只猫猫？",
                    onConfirm: () => DoSwitchCat(),
                    onCancel: () => { Destroy(activeConfirmDialog.gameObject); activeConfirmDialog = null; },
                    title: "切换猫猫",
                    confirmText: "确认",
                    cancelText: "取消"
                );
            }
            else
            {
                DoSwitchCat();
            }
        }

        private void DoSwitchCat()
        {
            if (activeConfirmDialog) { Destroy(activeConfirmDialog.gameObject); activeConfirmDialog = null; }
            if (CatManager.Instance != null)
                CatManager.Instance.SelectCat(previewIndex);

            // 切换猫猫时强制重置棋局（不扣体力，仅重置棋盘）
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.Restart();
                // 每次切换猫猫扣挑战费用
                var cat = CatManager.Instance?.Selected;
                if (cat != null && cat.challengeCost > 0 && EconomyManager.Instance != null)
                    EconomyManager.Instance.SpendCoins(cat.challengeCost);
            }
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
                        if (unlockHintText) unlockHintText.text = "解锁成功！";
                        BuildGrid();
                        Preview(previewIndex);
                    }
                    else if (!success)
                    {
                        if (unlockHintText) unlockHintText.text = "观看完整广告才能解锁";
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
