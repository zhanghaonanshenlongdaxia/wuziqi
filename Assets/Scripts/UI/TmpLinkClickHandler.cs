using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Wuziqi.UI
{
    /// <summary>挂在 TMP_Text 上，点击 &lt;link&gt; 标签时自动 OpenURL。</summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TmpLinkClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private TMP_Text tmpText;

        private void Awake()
        {
            tmpText = GetComponent<TMP_Text>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (tmpText == null) return;
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(tmpText, eventData.position, eventData.pressEventCamera);
            if (linkIndex < 0) return;
            var linkInfo = tmpText.textInfo.linkInfo[linkIndex];
            Application.OpenURL(linkInfo.GetLinkID());
        }
    }
}
