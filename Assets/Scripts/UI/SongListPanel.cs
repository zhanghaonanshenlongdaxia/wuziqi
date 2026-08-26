using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Wuziqi.Game;

namespace Wuziqi.UI
{
    /// <summary>歌曲列表弹窗：用 SongItem 模板实例化曲目，选择播放，花仙喵币解锁。</summary>
    public class SongListPanel : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private Transform listRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private MusicPlayerUI player;
        [SerializeField] private SongItem songItemPrefab;

        // 曲目解锁配置（与 MusicPlayerUI tracks 对应）
        // 前3首免费，4-6首10仙喵币，7-9首20仙喵币
        private static readonly int[] coinCosts = { 0, 0, 0, 10, 10, 10, 20, 20, 20 };
        private const string K_Prefix = "Wuziqi.SongUnlock.";

        public static bool IsSongUnlocked(int index)
        {
            if (index < 0 || index >= coinCosts.Length) return true;
            if (coinCosts[index] == 0) return true;
            return PlayerPrefs.GetInt(K_Prefix + index, 0) == 1;
        }

        private void Start()
        {
            if (closeButton) closeButton.onClick.AddListener(Close);
        }

        private void OnEnable() => BuildList();

        private bool building; // 防止同帧重复构建

        private MusicPlayerUI.Track[] GetTracks()
        {
            if (player == null) return null;
            var f = typeof(MusicPlayerUI).GetField("tracks",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (MusicPlayerUI.Track[])f.GetValue(player);
        }

        private void BuildList()
        {
            if (listRoot == null || building) return;
            building = true;
            var tracks = GetTracks();
            if (tracks == null) { building = false; return; }

            // clear（DestroyImmediate 保证清理立即生效）
            for (int i = listRoot.childCount - 1; i >= 0; i--)
                DestroyImmediate(listRoot.GetChild(i).gameObject);

            for (int i = 0; i < tracks.Length; i++)
            {
                int idx = i;
                var t = tracks[i];
                bool unlocked = IsSongUnlocked(i);
                int cost = i < coinCosts.Length ? coinCosts[i] : 0;

                SongItem item;
                if (songItemPrefab != null)
                    item = Instantiate(songItemPrefab, listRoot);
                else
                    item = new GameObject("Song", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(SongItem)).GetComponent<SongItem>();

                item.SetTitle(unlocked ? $"♪ {t.title}" : $"♪ ???（未解锁）");
                item.SetStatus(unlocked ? "▶ 播放" : $"{cost} 仙喵币");
                item.Button.onClick.AddListener(() => OnSongClicked(idx));
            }
            building = false;
        }

        private void OnSongClicked(int idx)
        {
            if (IsSongUnlocked(idx))
            {
                PlaySong(idx);
                Close();
                return;
            }
            int cost = idx < coinCosts.Length ? coinCosts[idx] : 0;
            if (EconomyManager.Instance != null && EconomyManager.Instance.SpendCoins(cost))
            {
                PlayerPrefs.SetInt(K_Prefix + idx, 1);
                PlayerPrefs.Save();
                PlaySong(idx);
                Close();
            }
            else
            {
                // 余额不足：刷新状态提示
                BuildList();
            }
        }

        private void PlaySong(int idx)
        {
            if (player == null) return;
            var m = typeof(MusicPlayerUI).GetMethod("Play",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            m?.Invoke(player, new object[] { idx });
        }

        private void Close()
        {
            var tbc = FindAnyObjectByType<TopBarController>();
            if (tbc != null) tbc.CloseAllPanels();
            else gameObject.SetActive(false);
        }
    }
}
