using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Wuziqi.UI
{
    /// <summary>启动闪屏：黑底 + 工作室 Logo + 名称，淡入→停留→淡出，结束后激活主界面。</summary>
    public class SplashScreen : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private GameObject mainMenuRoot;

        [Header("时长")]
        [SerializeField] private float fadeInTime = 1f;
        [SerializeField] private float holdTime = 2f;
        [SerializeField] private float fadeOutTime = 1f;

        private void Start()
        {
            if (canvasGroup) canvasGroup.alpha = 0f;
            if (mainMenuRoot) mainMenuRoot.SetActive(false);
            StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            // 淡入
            yield return Fade(0f, 1f, fadeInTime);

            // 停留
            yield return new WaitForSeconds(holdTime);

            // 淡出
            yield return Fade(1f, 0f, fadeOutTime);

            // 切换到主界面 + 播放菜单 BGM
            gameObject.SetActive(false);
            if (mainMenuRoot) mainMenuRoot.SetActive(true);

            var bgm = GameObject.Find("BGMPlayer");
            if (bgm)
            {
                var src = bgm.GetComponent<AudioSource>();
                if (src && !src.isPlaying) src.Play();
            }
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (canvasGroup == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = to;
        }
    }
}
