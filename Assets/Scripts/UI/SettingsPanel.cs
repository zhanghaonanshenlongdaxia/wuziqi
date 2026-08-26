using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Wuziqi.UI
{
    /// <summary>音乐/音效开关弹窗（水墨图钉式开关：Button+On/Off两张sprite切换）。</summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("开关（Button+Image，切sprite）")]
        [SerializeField] private Button musicToggle;
        [SerializeField] private Button sfxToggle;
        [SerializeField] private Sprite toggleOnSprite;
        [SerializeField] private Sprite toggleOffSprite;

        [Header("音量滑条")]
        [SerializeField] private Slider musicVolume;
        [SerializeField] private Slider sfxVolume;

        [Header("关闭")]
        [SerializeField] private Button closeButton;

        private const string K_MusicOn = "Wuziqi.MusicOn";
        private const string K_SFXOn = "Wuziqi.SFXOn";
        private const string K_MusicVol = "Wuziqi.MusicVolume";
        private const string K_SFXVol = "Wuziqi.SFXVolume";

        private bool musicOn = true;
        private bool sfxOn = true;

        private void Start()
        {
            if (closeButton) closeButton.onClick.AddListener(Close);

            musicOn = PlayerPrefs.GetInt(K_MusicOn, 1) == 1;
            sfxOn = PlayerPrefs.GetInt(K_SFXOn, 1) == 1;

            if (musicToggle) musicToggle.onClick.AddListener(OnMusicToggle);
            if (sfxToggle) sfxToggle.onClick.AddListener(OnSFXToggle);
            if (musicVolume) { musicVolume.value = PlayerPrefs.GetFloat(K_MusicVol, 1f); musicVolume.onValueChanged.AddListener(OnMusicVolume); }
            if (sfxVolume) { sfxVolume.value = PlayerPrefs.GetFloat(K_SFXVol, 1f); sfxVolume.onValueChanged.AddListener(OnSFXVolume); }

            RefreshToggleVisuals();
            ApplyAudioSettings();
        }

        private void OnMusicToggle()
        {
            musicOn = !musicOn;
            PlayerPrefs.SetInt(K_MusicOn, musicOn ? 1 : 0);
            PlayerPrefs.Save();
            RefreshToggleVisuals();
            ApplyAudioSettings();
        }

        private void OnSFXToggle()
        {
            sfxOn = !sfxOn;
            PlayerPrefs.SetInt(K_SFXOn, sfxOn ? 1 : 0);
            PlayerPrefs.Save();
            RefreshToggleVisuals();
            ApplyAudioSettings();
        }

        private void RefreshToggleVisuals()
        {
            if (musicToggle != null && toggleOnSprite != null && toggleOffSprite != null)
                musicToggle.GetComponent<Image>().sprite = musicOn ? toggleOnSprite : toggleOffSprite;
            if (sfxToggle != null && toggleOnSprite != null && toggleOffSprite != null)
                sfxToggle.GetComponent<Image>().sprite = sfxOn ? toggleOnSprite : toggleOffSprite;
        }

        private void OnMusicVolume(float v)
        {
            PlayerPrefs.SetFloat(K_MusicVol, v);
            PlayerPrefs.Save();
            ApplyAudioSettings();
        }

        private void OnSFXVolume(float v)
        {
            PlayerPrefs.SetFloat(K_SFXVol, v);
            PlayerPrefs.Save();
            ApplyAudioSettings();
        }

        private void ApplyAudioSettings()
        {
            bool mOn = PlayerPrefs.GetInt(K_MusicOn, 1) == 1;
            bool sOn = PlayerPrefs.GetInt(K_SFXOn, 1) == 1;
            float mVol = PlayerPrefs.GetFloat(K_MusicVol, 1f);
            float sVol = PlayerPrefs.GetFloat(K_SFXVol, 1f);

            var musicPlayer = FindAnyObjectByType<MusicPlayerUI>();
            if (musicPlayer != null)
            {
                var src = typeof(MusicPlayerUI).GetField("source", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(musicPlayer) as AudioSource;
                if (src != null) { src.mute = !mOn; src.volume = mVol; }
            }
            AudioListener.volume = sOn ? sVol : 0f;
        }

        private void Close()
        {
            var tbc = FindAnyObjectByType<TopBarController>();
            if (tbc != null) tbc.CloseAllPanels();
            else gameObject.SetActive(false);
        }
    }
}
