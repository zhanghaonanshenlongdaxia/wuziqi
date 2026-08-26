using UnityEngine;

namespace Wuziqi.Game
{
    /// <summary>Unity Ads 占位管理器：接口已就绪，后续接 SDK 时替换实现。</summary>
    public class AdManager : MonoBehaviour
    {
        public static AdManager Instance { get; private set; }

        [Header("调试")]
        [Tooltip("勾选后广告直接返回成功（不弹真实广告），方便测试。后续接 SDK 后改为 false。")]
        public bool simulateAds = true;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>展示激励广告，完成后回调 onComplete(true=成功)。</summary>
        public void ShowRewarded(string placementId, System.Action<bool> onComplete)
        {
            Debug.Log($"[AdManager] ShowRewarded: placement={placementId}, simulated={simulateAds}");
            if (simulateAds)
            {
                onComplete?.Invoke(true);
                return;
            }
            // TODO: Unity Ads SDK
            // var options = new Ads.Unity.ShowOptions { resultCallback = (result) => { ... } };
            // Advertisement.Show(placementId, options);
            onComplete?.Invoke(true);
        }

        /// <summary>展示插屏广告。</summary>
        public void ShowInterstitial(System.Action onComplete)
        {
            Debug.Log("[AdManager] ShowInterstitial: simulated");
            onComplete?.Invoke();
        }

        public bool IsRewardedReady(string placementId) => true;
    }
}
