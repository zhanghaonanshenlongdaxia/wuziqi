using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Wuziqi.UI
{
    /// <summary>国风音乐播放器：曲名显示、顺序/随机/单曲模式、暂停、下一首、声浪可视化。</summary>
    public class MusicPlayerUI : MonoBehaviour
    {
        public enum PlayMode { Sequential, Shuffle, Single }

        [System.Serializable]
        public class Track
        {
            public AudioClip clip;
            public string title;
        }

        [Header("引用")]
        [SerializeField] private AudioSource source;
        [SerializeField] private Track[] tracks;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text modeText;
        [SerializeField] private Button playPauseButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button modeButton;
        [SerializeField] private RectTransform waveRoot;
        [SerializeField] private int waveBars = 24;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip clickClip;
        [SerializeField] private Image modeIcon;
        [SerializeField] private Sprite sequentialIcon;
        [SerializeField] private Sprite shuffleIcon;
        [SerializeField] private Sprite singleIcon;

        private static readonly Dictionary<PlayMode, string> ModeLabels = new Dictionary<PlayMode, string>
        {
            { PlayMode.Sequential, "顺序" },
            { PlayMode.Shuffle, "随机" },
            { PlayMode.Single, "单曲" },
        };

        private PlayMode mode = PlayMode.Sequential;
        private int index;
        private readonly List<Image> bars = new List<Image>();
        private readonly float[] samples = new float[64];
        private readonly Color inkColor = new Color32(59, 56, 51, 255);

        private void Start()
        {
            if (source == null)
            {
                GameObject old = GameObject.Find("BGMPlayer");
                if (old != null && old.TryGetComponent(out AudioSource oldSrc))
                {
                    source = gameObject.AddComponent<AudioSource>();
                    source.volume = oldSrc.volume;
                    oldSrc.Stop();
                    oldSrc.enabled = false;
                }
            }
            if (source == null) source = gameObject.AddComponent<AudioSource>();
            source.loop = false;
            source.playOnAwake = false;

            playPauseButton.onClick.AddListener(TogglePause);
            nextButton.onClick.AddListener(PlayNext);
            modeButton.onClick.AddListener(CycleMode);

            BuildWaveBars();
            UpdateModeLabel();
            Play(0);
        }

        private void Update()
        {
            if (source.isPlaying && source.clip != null && source.time >= source.clip.length - 0.05f)
                OnTrackFinished();
            AnimateWave();
        }

        // ---------- 播放控制 ----------

        private void Play(int i)
        {
            if (tracks == null || tracks.Length == 0) return;
            index = Mathf.Clamp(i, 0, tracks.Length - 1);
            var t = tracks[index];
            if (t == null || t.clip == null) return;
            source.clip = t.clip;
            source.Play();
            if (titleText != null) titleText.text = $"♪ {t.title}";
        }

        public void TogglePause()
        {
            PlayClick();
            if (source.isPlaying) source.Pause();
            else source.UnPause();
        }

        public void PlayNext()
        {
            PlayClick();
            if (tracks == null || tracks.Length == 0) return;
            if (mode == PlayMode.Shuffle && tracks.Length > 1)
            {
                int r;
                do { r = Random.Range(0, tracks.Length); } while (r == index);
                Play(r);
            }
            else Play((index + 1) % tracks.Length);
        }

        public void CycleMode()
        {
            PlayClick();
            mode = mode == PlayMode.Sequential ? PlayMode.Shuffle
                 : mode == PlayMode.Shuffle ? PlayMode.Single
                 : PlayMode.Sequential;
            UpdateModeLabel();
        }

        private void PlayClick()
        {
            if (sfxSource != null && clickClip != null) sfxSource.PlayOneShot(clickClip);
        }

        private void OnTrackFinished()
        {
            switch (mode)
            {
                case PlayMode.Single:
                    source.time = 0f;
                    source.Play();
                    break;
                case PlayMode.Shuffle:
                    PlayNext();
                    break;
                default:
                    Play((index + 1) % tracks.Length);
                    break;
            }
        }

        private void UpdateModeLabel()
        {
            if (modeText != null) modeText.text = ModeLabels[mode];
            if (modeIcon != null)
            {
                var sprite = mode == PlayMode.Sequential ? sequentialIcon
                           : mode == PlayMode.Shuffle ? shuffleIcon
                           : singleIcon;
                if (sprite != null) modeIcon.sprite = sprite;
            }
        }

        // ---------- 声浪 ----------

        private void BuildWaveBars()
        {
            if (waveRoot == null) return;
            foreach (Transform c in waveRoot) Destroy(c.gameObject);
            bars.Clear();

            for (int i = 0; i < waveBars; i++)
            {
                var go = new GameObject($"Bar_{i:00}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(waveRoot, false);
                var rt = (RectTransform)go.transform;
                float step = 1f / waveBars;
                rt.anchorMin = new Vector2(step * i, 0.5f);
                rt.anchorMax = new Vector2(step * (i + 1), 0.5f);
                rt.offsetMin = new Vector2(1.5f, -3f);
                rt.offsetMax = new Vector2(-1.5f, 3f);
                var img = go.GetComponent<Image>();
                img.color = inkColor;
                img.raycastTarget = false;
                bars.Add(img);
            }
        }

        private void AnimateWave()
        {
            if (bars.Count == 0 || source == null) return;
            if (!source.isPlaying)
            {
                for (int i = 0; i < bars.Count; i++)
                    SetBarHeight(i, Mathf.Lerp(GetBarHeight(i), 3f, Time.deltaTime * 8f));
                return;
            }

            source.GetSpectrumData(samples, 0, FFTWindow.Blackman);
            int chunk = samples.Length / bars.Count;
            for (int i = 0; i < bars.Count; i++)
            {
                float sum = 0f;
                for (int j = 0; j < chunk; j++) sum += samples[i * chunk + j];
                float avg = sum / chunk;
                // 感知加权：低频段能量大，指数放大，高度 3~26
                float target = Mathf.Clamp(Mathf.Sqrt(avg) * 340f, 3f, 26f);
                float smooth = Mathf.Lerp(GetBarHeight(i), target, Time.deltaTime * 14f);
                SetBarHeight(i, smooth);
            }
        }

        private float GetBarHeight(int i)
        {
            // half-height (offsetMax.y = +h, offsetMin.y = -h)
            return bars[i].rectTransform.offsetMax.y;
        }

        private void SetBarHeight(int i, float h)
        {
            var rt = bars[i].rectTransform;
            rt.offsetMin = new Vector2(rt.offsetMin.x, -h);
            rt.offsetMax = new Vector2(rt.offsetMax.x, h);
        }
    }
}
