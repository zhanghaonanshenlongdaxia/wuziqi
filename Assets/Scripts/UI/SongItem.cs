using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Wuziqi.UI
{
    /// <summary>歌曲列表条目模板：数据填充由 SongListPanel 调用。</summary>
    public class SongItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button button;

        public Button Button => button;

        public void SetTitle(string title) { if (titleText) titleText.text = title; }
        public void SetStatus(string status) { if (statusText) statusText.text = status; }
        public void SetInteractable(bool value) { if (button) button.interactable = value; }
    }
}
