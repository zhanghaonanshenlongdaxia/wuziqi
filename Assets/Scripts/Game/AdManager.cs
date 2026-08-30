using UnityEngine;
using Dirichlet.Mediation;

namespace Wuziqi.Game
{
    /// <summary>
    /// 广告管理器：基于 Dirichlet 聚合 SDK，支持激励视频和插屏广告。
    /// simulateAds=true 时跳过真实广告，方便编辑器调试。
    /// </summary>
    public class AdManager : MonoBehaviour
    {
        public static AdManager Instance { get; private set; }

        [Header("调试")]
        [Tooltip("勾选后广告直接返回成功（不弹真实广告），方便测试。")]
        public bool simulateAds = true;

        [Header("Dirichlet 配置")]
        [Tooltip("在 Dirichlet 后台创建应用后获取")]
        public long mediaId = 1104883;
        public string mediaName = "仙喵五子棋";
        public string mediaKey = "dAsCddft4lgmAn6yuVeyiKbcP32a2hKtifiX4iPztkXVwqrOW4qJssZxobJsaVBVWZopMZiVfhCt84818Td9w40dqRINqsPs22JWd5dMJm6QaF3I10cl71pqbjQbHYxY";
        public string gameChannel = "domestic";
        public string tapClientId = "";

        [Header("广告位 ID")]
        public string rewardSlot = "1060035";
        public string interstitialSlot = "1060074";

        private DirichletAdNative adNative;
        private bool sdkInitialized;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (!simulateAds)
                InitSdk();
        }

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
                });
        }

        #endregion

        #region 激励视频

        /// <summary>展示激励广告，完成后回调 onComplete(true=成功观看，false=失败/跳过)。</summary>
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

            if (!long.TryParse(rewardSlot, out var spaceId))
            {
                Debug.LogError($"[AdManager] 激励广告位 ID 格式错误: {rewardSlot}");
                onComplete?.Invoke(false);
                return;
            }

            var userId = SystemInfo.deviceUniqueIdentifier;
            var request = new DirichletAdRequest.Builder()
                .WithSpaceId(spaceId)
                .WithUserId(userId)
                .WithRewardName("体力")
                .WithRewardAmount(1)
                .Build();

            adNative.LoadRewardVideoAd(request,
                ad =>
                {
                    // 加载成功，监听事件后展示
                    ad.RewardVerified += args =>
                    {
                        Debug.Log($"[AdManager] 激励验证: verified={args.IsVerified}, amount={args.RewardAmount}");
                    };
                    ad.Closed += () =>
                    {
                        Debug.Log("[AdManager] 激励广告关闭");
                        onComplete?.Invoke(true);
                    };
                    ad.Clicked += () => Debug.Log("[AdManager] 激励广告点击");

                    var shown = ad.Show();
                    if (!shown)
                    {
                        Debug.LogWarning("[AdManager] 激励广告展示失败");
                        onComplete?.Invoke(false);
                    }
                },
                error =>
                {
                    Debug.LogError($"[AdManager] 激励广告加载失败: {error}");
                    onComplete?.Invoke(false);
                });
        }

        /// <summary>使用自动激励广告（Dirichlet SDK 自带加载+展示）。</summary>
        public void ShowRewardedAuto(string placementId, System.Action<bool> onComplete)
        {
            Debug.Log($"[AdManager] ShowRewardedAuto: placement={placementId}");

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

            if (!long.TryParse(rewardSlot, out var spaceId))
            {
                Debug.LogError($"[AdManager] 激励广告位 ID 格式错误: {rewardSlot}");
                onComplete?.Invoke(false);
                return;
            }

            var userId = SystemInfo.deviceUniqueIdentifier;
            var request = new DirichletAdRequest.Builder()
                .WithSpaceId(spaceId)
                .WithUserId(userId)
                .WithRewardName("体力")
                .WithRewardAmount(1)
                .Build();

            adNative.ShowRewardVideoAutoAd(request, new RewardAutoListener(onComplete));
        }

        private sealed class RewardAutoListener : IDirichletRewardVideoAutoAdListener
        {
            private readonly System.Action<bool> onComplete;

            public RewardAutoListener(System.Action<bool> onComplete) => this.onComplete = onComplete;

            public void OnError(DirichletError error)
            {
                Debug.LogError($"[AdManager] 自动激励失败: {error}");
                onComplete?.Invoke(false);
            }

            public void OnAdShow() => Debug.Log("[AdManager] 自动激励展示");
            public void OnAdClick() => Debug.Log("[AdManager] 自动激励点击");

            public void OnAdClose()
            {
                Debug.Log("[AdManager] 自动激励关闭");
                onComplete?.Invoke(true);
            }

            public void OnRewardVerify(DirichletRewardVerificationEventArgs args)
            {
                Debug.Log($"[AdManager] 自动激励验证: verified={args.IsVerified}, amount={args.RewardAmount}");
            }
        }

        #endregion

        #region 插屏广告

        /// <summary>展示插屏广告。</summary>
        public void ShowInterstitial(System.Action onComplete)
        {
            Debug.Log($"[AdManager] ShowInterstitial: simulated={simulateAds}");

            if (simulateAds)
            {
                onComplete?.Invoke();
                return;
            }

            if (!sdkInitialized || adNative == null)
            {
                Debug.LogWarning("[AdManager] SDK 未初始化，回退模拟");
                onComplete?.Invoke();
                return;
            }

            if (!long.TryParse(interstitialSlot, out var spaceId))
            {
                Debug.LogError($"[AdManager] 插屏广告位 ID 格式错误: {interstitialSlot}");
                onComplete?.Invoke();
                return;
            }

            var request = new DirichletAdRequest.Builder()
                .WithSpaceId(spaceId)
                .Build();

            adNative.LoadInterstitialAd(request,
                ad =>
                {
                    ad.Closed += () =>
                    {
                        Debug.Log("[AdManager] 插屏广告关闭");
                        onComplete?.Invoke();
                    };
                    ad.Clicked += () => Debug.Log("[AdManager] 插屏广告点击");

                    var shown = ad.Show();
                    if (!shown)
                    {
                        Debug.LogWarning("[AdManager] 插屏广告展示失败");
                        onComplete?.Invoke();
                    }
                },
                error =>
                {
                    Debug.LogError($"[AdManager] 插屏广告加载失败: {error}");
                    onComplete?.Invoke();
                });
        }

        #endregion

        public bool IsRewardedReady(string placementId) => simulateAds || sdkInitialized;
    }
}
