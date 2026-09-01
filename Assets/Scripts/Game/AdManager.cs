using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Dirichlet.Mediation;

namespace Wuziqi.Game
{
    /// <summary>
    /// 广告管理器：基于 Dirichlet 聚合 SDK，支持激励视频和插屏广告。
    /// 配置从 StreamingAssets/dirichlet_keys.json 读取，未找到时使用 Inspector 默认值。
    /// Editor 下自动模拟，真机使用真实广告。
    /// </summary>
    public class AdManager : MonoBehaviour
    {
        public static AdManager Instance { get; private set; }

        [Header("调试")]
        [Tooltip("勾选后广告直接返回成功（不弹真实广告），方便测试。")]
        public bool simulateAds = false;

        [Header("Dirichlet 配置（从 dirichlet_keys.json 自动加载）")]
        public long mediaId = 1107264;
        public string mediaName = "喵仙五子棋";
        public string mediaKey = "";
        public string gameChannel = "domestic";
        public string tapClientId = "";

        [Header("广告位 ID")]
        public long rewardSpaceId = 1063337;
        public long interstitialSpaceId = 0;

        private DirichletAdNative adNative;
        private bool sdkInitialized;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
            simulateAds = true;
#endif
        }

        private IEnumerator Start()
        {
            // 先从 StreamingAssets/dirichlet_keys.json 加载密钥
            yield return LoadKeysFromJson();

            if (simulateAds)
            {
                Debug.Log("[AdManager] 模拟模式，跳过 SDK 初始化");
                yield break;
            }

            if (string.IsNullOrEmpty(mediaKey) || mediaKey.Length < 20)
            {
                Debug.LogWarning("[AdManager] MediaKey 未配置！请将 dirichlet_keys.json 放到 Assets/StreamingAssets/");
                simulateAds = true;
                yield break;
            }

            if (rewardSpaceId <= 0)
            {
                Debug.LogWarning("[AdManager] 激励视频 SpaceId 未配置");
                simulateAds = true;
                yield break;
            }

            InitSdk();
        }

        #region 配置加载

        /// <summary>
        /// 从 StreamingAssets/dirichlet_keys.json 加载密钥配置。
        /// </summary>
        private IEnumerator LoadKeysFromJson()
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, "dirichlet_keys.json");

            using (var www = UnityWebRequest.Get(path))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("[AdManager] 未找到 dirichlet_keys.json，使用 Inspector 默认配置");
                    yield break;
                }

                string json = www.downloadHandler.text;
                if (string.IsNullOrEmpty(json))
                {
                    yield break;
                }

                try
                {
                    var keys = JsonUtility.FromJson<DirichletKeys>(json);
                    if (keys == null) yield break;

                    if (keys.mediaId > 0) mediaId = keys.mediaId;
                    if (!string.IsNullOrEmpty(keys.mediaName)) mediaName = keys.mediaName;
                    if (!string.IsNullOrEmpty(keys.mediaKey)) mediaKey = keys.mediaKey;
                    if (!string.IsNullOrEmpty(keys.gameChannel)) gameChannel = keys.gameChannel;
                    if (!string.IsNullOrEmpty(keys.tapClientId)) tapClientId = keys.tapClientId;
                    if (keys.rewardSpaceId > 0) rewardSpaceId = keys.rewardSpaceId;
                    if (keys.interstitialSpaceId > 0) interstitialSpaceId = keys.interstitialSpaceId;

                    Debug.Log("[AdManager] 已从 dirichlet_keys.json 加载配置");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[AdManager] 解析 dirichlet_keys.json 失败: {ex.Message}");
                }
            }
        }

        [System.Serializable]
        private class DirichletKeys
        {
            public long mediaId;
            public string mediaName;
            public string mediaKey;
            public string gameChannel;
            public string tapClientId;
            public long rewardSpaceId;
            public long interstitialSpaceId;
        }

        #endregion

        #region SDK 初始化

        private void InitSdk()
        {
            var config = new DirichletAdConfig.Builder()
                .WithMediaId(mediaId)
                .WithMediaName(mediaName)
                .WithMediaKey(mediaKey)
                .WithGameChannel(gameChannel)
                .WithTapClientId(tapClientId)
                .EnableDebug(true)
                .ShakeEnabled(true)
                .Build();

            DirichletSdk.Init(config,
                result =>
                {
                    sdkInitialized = true;
                    adNative = DirichletAdManager.CreateAdNative();
                    DirichletSdk.RequestPermissionIfNecessary();
                    Debug.Log("[AdManager] Dirichlet SDK 初始化成功");
                },
                error =>
                {
                    Debug.LogError($"[AdManager] Dirichlet SDK 初始化失败: {error}");
                    simulateAds = true;
                });
        }

        #endregion

        #region 激励视频

        /// <summary>
        /// 展示激励广告，完成后回调 onComplete(true=成功观看并验证，false=失败/跳过)。
        /// 使用自动加载+展示 API，更可靠。
        /// </summary>
        public void ShowRewarded(string placementId, System.Action<bool> onComplete)
        {
            Debug.Log($"[AdManager] ShowRewarded: placement={placementId}, simulated={simulateAds}");

            if (simulateAds)
            {
                onComplete?.Invoke(true);
                return;
            }

            if (!sdkInitialized || adNative == null)
            {
                Debug.LogWarning("[AdManager] SDK 未初始化，回退模拟");
                onComplete?.Invoke(true);
                return;
            }

            var userId = SystemInfo.deviceUniqueIdentifier;
            var request = new DirichletAdRequest.Builder()
                .WithSpaceId(rewardSpaceId)
                .WithUserId(userId)
                .WithRewardName("体力")
                .WithRewardAmount(10)
                .Build();

            adNative.ShowRewardVideoAutoAd(request, new RewardAutoListener(onComplete));
        }

        private sealed class RewardAutoListener : IDirichletRewardVideoAutoAdListener
        {
            private readonly System.Action<bool> onComplete;
            private bool rewarded;

            public RewardAutoListener(System.Action<bool> onComplete) => this.onComplete = onComplete;

            public void OnError(DirichletError error)
            {
                Debug.LogError($"[AdManager] 激励广告失败: {error}");
                onComplete?.Invoke(false);
            }

            public void OnAdShow()
            {
                Debug.Log("[AdManager] 激励广告展示");
            }

            public void OnAdClick()
            {
                Debug.Log("[AdManager] 激励广告点击");
            }

            public void OnRewardVerify(DirichletRewardVerificationEventArgs args)
            {
                rewarded = args.IsVerified;
                Debug.Log($"[AdManager] 激励验证: verified={args.IsVerified}, amount={args.RewardAmount}, name={args.RewardName}");
            }

            public void OnAdClose()
            {
                Debug.Log($"[AdManager] 激励广告关闭, rewarded={rewarded}");
                onComplete?.Invoke(rewarded);
            }
        }

        #endregion

        #region 插屏广告

        /// <summary>展示插屏广告。</summary>
        public void ShowInterstitial(System.Action onComplete)
        {
            Debug.Log($"[AdManager] ShowInterstitial: simulated={simulateAds}");

            if (simulateAds || interstitialSpaceId <= 0)
            {
                onComplete?.Invoke();
                return;
            }

            if (!sdkInitialized || adNative == null)
            {
                Debug.LogWarning("[AdManager] SDK 未初始化");
                onComplete?.Invoke();
                return;
            }

            var request = new DirichletAdRequest.Builder()
                .WithSpaceId(interstitialSpaceId)
                .Build();

            adNative.ShowInterstitialAutoAd(request, new InterstitialAutoListener(onComplete));
        }

        private sealed class InterstitialAutoListener : IDirichletInterstitialAutoAdListener
        {
            private readonly System.Action onComplete;

            public InterstitialAutoListener(System.Action onComplete) => this.onComplete = onComplete;

            public void OnError(DirichletError error)
            {
                Debug.LogError($"[AdManager] 插屏广告失败: {error}");
                onComplete?.Invoke();
            }

            public void OnAdShow() => Debug.Log("[AdManager] 插屏广告展示");
            public void OnAdClick() => Debug.Log("[AdManager] 插屏广告点击");

            public void OnAdClose()
            {
                Debug.Log("[AdManager] 插屏广告关闭");
                onComplete?.Invoke();
            }
        }

        #endregion

        public bool IsRewardedReady(string placementId) => simulateAds || sdkInitialized;
    }
}
