using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Wuziqi.UI
{
    /// <summary>
    /// 读取 Animator 的 normalizedTime，驱动 Image/SpriteRenderer 换帧。
    /// 由 CharacterController 通过 SetMood() 切换状态。
    /// </summary>
    public class SpriteAnimator : MonoBehaviour
    {
        [HideInInspector] public float frameIndex;

        private SpriteRenderer sr;
        private Image img;
        private Animator anim;
        private Dictionary<int, Sprite[]> moodFrames = new Dictionary<int, Sprite[]>();
        private int currentMood = -1;
        private Sprite[] currentFrames;

        private static readonly int MoodHash = Animator.StringToHash("Mood");

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            img = GetComponent<Image>();
            anim = GetComponent<Animator>();
        }

        /// <summary>设置某个 mood 的帧数据。</summary>
        public void SetFrames(int mood, Sprite[] frames)
        {
            moodFrames[mood] = frames;
        }

        /// <summary>切换 mood，Animator 切状态，帧数据立即生效。</summary>
        public void SetMood(int mood)
        {
            currentMood = mood;
            moodFrames.TryGetValue(mood, out currentFrames);
            if (anim != null) anim.SetInteger(MoodHash, mood);
        }

        private void Update()
        {
            if (currentFrames == null || currentFrames.Length == 0) return;
            if (anim == null) return;

            var stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            float normalizedTime = stateInfo.normalizedTime % 1f;
            int index = Mathf.FloorToInt(normalizedTime * currentFrames.Length);
            index = Mathf.Clamp(index, 0, currentFrames.Length - 1);

            var target = currentFrames[index];
            if (img != null) { if (img.sprite != target) img.sprite = target; }
            else if (sr != null) { if (sr.sprite != target) sr.sprite = target; }
        }
    }
}
