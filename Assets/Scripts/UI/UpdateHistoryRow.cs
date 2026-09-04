using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Wuziqi.UI
{
    /// <summary>更新历史条目模板：数据填充由 UpdateHistoryPanel 调用（同 SongItem 惯例）。</summary>
    public class UpdateHistoryRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text verText;     // 版本号
        [SerializeField] private TMP_Text dateText;    // 日期
        [SerializeField] private TMP_Text titleText;   // 版本主题
        [SerializeField] private TMP_Text changesText; // 更新内容（多行）

        public void SetData(string ver, string date, string title, string changes)
        {
            if (verText) verText.text = ver;
            if (dateText) dateText.text = date;
            if (titleText) titleText.text = title;
            if (changesText) changesText.text = changes;
        }
    }
}
