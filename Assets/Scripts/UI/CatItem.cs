using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Wuziqi.UI
{
    /// <summary>猫猫选择格位模板：头像+名字+锁定遮罩，数据填充由 CatSelectPanel 调用。</summary>
    public class CatItem : MonoBehaviour
    {
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private Button button;

        public Button Button => button;

        public void SetPortrait(Sprite sprite) { if (portrait && sprite) portrait.sprite = sprite; }
        public void SetName(string name) { if (nameText) nameText.text = name; }
        public void SetLocked(bool locked) { if (lockOverlay) lockOverlay.SetActive(locked); }
        public void SetSelected(bool selected) { }
    }
}
