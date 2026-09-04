using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Wuziqi.UI
{
    /// <summary>更新历史面板：从预制体实例化，列出各版本更新内容（同 AchievementPanel 惯例）。</summary>
    public class UpdateHistoryPanel : MonoBehaviour
    {
        [SerializeField] private Transform listRoot;   // Scroll/Viewport/Content
        [SerializeField] private Button closeButton;   // 关闭按钮
        [SerializeField] private UpdateHistoryRow rowPrefab; // 行模板

        // 版本历史（新→旧；发新版时在最上面加一条即可）
        private static readonly (string ver, string date, string title, string changes)[] History =
        {
            ("版本 1.2.1 (19)", "2026.09.04", "棋逢对手",
                "复仇挑战：输过的猫猫可以复仇，AI 按当时的棋谱重演走法\n段位系统：棋童 → 仙喵棋圣，记录你的成长\n更新历史面板（你正在看的这个）\nSteam 式成就弹窗：图标 + 详情 + 解锁音效\n隐藏成就：喵仙人提款机（连败 5 局解锁）"),
            ("版本 1.2.1 (17)", "2026.09.03", "灵智觉醒",
                "成就系统：17 枚成就 + 水墨图标 + 图集 + Steam 式左下角弹窗 + 解锁音效\n道具系统：提示卡 / 双倍卡（库存 0 自动提醒购买价）\n新手保护：连败橡皮筋（连败越多 AI 越\"放水\"）+ 连败 3 局送提示卡 + 猫台词安抚"),
            ("版本 1.2.1 (17)", "2026.09.03", "明察秋毫",
                "1.修复游戏界面宽屏适配，布局更合理\n2.新增二次确认落子功能，避免手机落子错误（水墨对号 + 高光预览）\n下次大版本更新：将新增成就系统"),
            ("版本 1.2.1 (15)", "2026.09.02", "棋力精进",
                "优化了猫猫的难度等级，现在难度正常了，后面的猫猫越难对付"),
            ("版本 1.2.1 (14)", "2026.09.01", "初入仙门",
                "游戏初版，当前可以和各种猫猫对局"),
        };

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
            if (listRoot == null || building) return;
            building = true;

            for (int i = listRoot.childCount - 1; i >= 0; i--)
                Destroy(listRoot.GetChild(i).gameObject);

            foreach (var e in History)
            {
                var row = Instantiate(rowPrefab, listRoot);
                row.SetData(e.ver, e.date, e.title, e.changes);
            }
            building = false;
        }
    }
}
