using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Wuziqi.Game;

namespace Wuziqi.UI
{
    /// <summary>成就面板预制体：引用 Content/Close，打开时重建条目列表（同 SongListPanel 惯例）。</summary>
    public class AchievementPanel : MonoBehaviour
    {
        [SerializeField] private Transform listRoot;      // Scroll/Viewport/Content
        [SerializeField] private Button closeButton;      // 关闭按钮
        [SerializeField] private AchievementItem achievementItemPrefab; // 条目模板（Assets/Prefabs/UI/AchievementItem.prefab）

        private bool building;

        private void Awake()
        {
            if (closeButton) closeButton.onClick.AddListener(Close);
        }

        private void OnEnable() => Rebuild();

        public void Show()
        {
            gameObject.SetActive(true);
            Rebuild();
        }

        public void Close() => Destroy(gameObject);

        private void Rebuild()
        {
            var mgr = AchievementsManager.Instance;
            var ps = PlayerStats.Instance;
            if (listRoot == null || mgr == null || ps == null || building) return;
            building = true;

            for (int i = listRoot.childCount - 1; i >= 0; i--)
                Destroy(listRoot.GetChild(i).gameObject);

            foreach (var def in mgr.Achievements)
            {
                if (def == null) continue;
                bool unlocked = ps.IsAchUnlocked(def.achId);
                bool showDetail = unlocked || !def.hidden;

                GameObject itemGo;
                if (achievementItemPrefab != null)
                    itemGo = Instantiate(achievementItemPrefab, listRoot).gameObject;
                else
                    itemGo = CreateFallbackRow(def.achId);
                var item = itemGo.GetComponent<AchievementItem>() ?? itemGo.AddComponent<AchievementItem>();
                item.SetName(showDetail ? def.displayName : "？？？");
                item.SetDesc(showDetail ? def.desc : "隐藏成就，继续探索喵…");
                item.SetReward(def.rewardCoins);
                if (unlocked)
                {
                    item.SetUnlocked(true);
                }
                else
                {
                    item.SetUnlocked(false);
                    item.SetProgress(Mathf.Min(mgr.GetProgress(def), def.targetValue), def.targetValue);
                }
            }
            building = false;
        }

        private GameObject CreateFallbackRow(string id)
        {
            var go = new GameObject($"Row_{id}", typeof(RectTransform));
            go.AddComponent<LayoutElement>().preferredHeight = 118f;
            return go;
        }
    }
}
