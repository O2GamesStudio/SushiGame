using UnityEngine;
using System;

public class UnityAdsManager : MonoBehaviour
{
    private static UnityAdsManager instance;
    private static bool isQuitting = false;

    public static UnityAdsManager Instance
    {
        get
        {
            if (isQuitting) return null;
            if (instance == null)
            {
                var go = new GameObject("UnityAdsManager");
                instance = go.AddComponent<UnityAdsManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    [SerializeField] private AdsConfig adsConfig;

    public event Action OnRewardEarned;
    public event Action OnAdClosed;
    public event Action OnAdFailedToLoad;
    public event Action OnAdFailedToShow;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (adsConfig == null) { Debug.LogError("[UnityAdsManager] AdsConfig가 없습니다."); return; }
        InitializeAdMob();
    }

    private void InitializeAdMob()
    {
        var go = new GameObject("AdMobManager");
        DontDestroyOnLoad(go);
        var mgr = go.AddComponent<AdMobManager>();
        mgr.Initialize(adsConfig.AdMobBannerUnitId, adsConfig.AdMobRewardedUnitId);

        mgr.OnRewardEarned += () => OnRewardEarned?.Invoke();
        mgr.OnAdClosed += () => OnAdClosed?.Invoke();
        mgr.OnAdFailedToLoad += () => OnAdFailedToLoad?.Invoke();
        mgr.OnAdFailedToShow += () => OnAdFailedToShow?.Invoke();
    }

    public void LoadRewardedAd() => AdMobManager.Instance?.LoadRewardedAd();
    public void ShowRewardedAd() => AdMobManager.Instance?.ShowRewardedAd();
    public void ShowBanner() => AdMobManager.Instance?.ShowBanner();
    public void HideBanner() => AdMobManager.Instance?.HideBanner();
    public void DestroyBanner() => AdMobManager.Instance?.DestroyBanner();

    public bool IsAdLoaded() => AdMobManager.Instance?.IsAdLoaded() ?? false;
    public bool IsBannerLoaded() => AdMobManager.Instance?.IsBannerLoaded() ?? false;
    public bool IsBannerDisplayed() => AdMobManager.Instance?.IsBannerDisplayed() ?? false;
    public bool IsLoadingAd() => false;
    public bool IsInitialized() => AdMobManager.Instance != null;

    public void ClearAllListeners()
    {
        OnRewardEarned = null;
        OnAdClosed = null;
        OnAdFailedToLoad = null;
        OnAdFailedToShow = null;
        AdMobManager.Instance?.ClearAllListeners();
    }

    private void OnApplicationQuit() => isQuitting = true;
}