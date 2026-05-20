using System;
using UnityEngine;
using GoogleMobileAds.Api;

public class AdMobManager : MonoBehaviour
{
    public static AdMobManager Instance { get; private set; }

    private string bannerUnitId;
    private string rewardedUnitId;
    private string interstitialUnitId;

    private BannerView bannerView;
    private RewardedAd rewardedAd;
    private InterstitialAd interstitialAd;
    private Action onInterstitialClosed;

    private bool isBannerLoaded = false;
    private bool isBannerDisplayed = false;
    private bool isRewardedLoaded = false;
    private bool isInterstitialLoaded = false;
    private bool pendingShowBanner = false;
    private bool isInitialized = false;
    private bool pendingShowBannerOnInit = false;

    public event Action OnRewardEarned;
    public event Action OnAdClosed;
    public event Action OnAdFailedToLoad;
    public event Action OnAdFailedToShow;

    public void Initialize(string bannerId, string rewardedId, string interstitialId)
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        bannerUnitId = bannerId;
        rewardedUnitId = rewardedId;
        interstitialUnitId = interstitialId;

        MobileAds.RaiseAdEventsOnUnityMainThread = true;

        MobileAds.Initialize(status =>
        {
            isInitialized = true;

            foreach (var adapter in status.getAdapterStatusMap())
                Debug.Log($"[AdMobManager] Adapter: {adapter.Key} / {adapter.Value.InitializationState}");

            LoadRewardedAd();
            LoadBannerAd();
            LoadInterstitialAd();

            if (pendingShowBannerOnInit)
            {
                pendingShowBannerOnInit = false;
                pendingShowBanner = true;
            }
        });
    }

    #region Banner

    private void LoadBannerAd()
    {
        bannerView?.Destroy();
        bannerView = new BannerView(bannerUnitId, AdSize.Banner, AdPosition.Bottom);

        bannerView.OnBannerAdLoaded += OnBannerLoaded;
        bannerView.OnBannerAdLoadFailed += OnBannerLoadFailed;
        bannerView.OnAdImpressionRecorded += OnBannerDisplayed;

        bannerView.LoadAd(new AdRequest());
    }

    private void OnBannerLoaded()
    {
        isBannerLoaded = true;
        if (pendingShowBanner)
        {
            pendingShowBanner = false;
            bannerView.Show();
        }
    }

    private void OnBannerLoadFailed(LoadAdError error)
    {
        isBannerLoaded = false;
        isBannerDisplayed = false;
        Debug.LogError($"[AdMobManager] 배너 로드 실패: {error.GetMessage()}");
    }

    private void OnBannerDisplayed() => isBannerDisplayed = true;

    public void ShowBanner()
    {
        if (!isInitialized) { pendingShowBannerOnInit = true; return; }
        if (bannerView == null) { LoadBannerAd(); pendingShowBanner = true; return; }
        if (isBannerLoaded) bannerView.Show();
        else pendingShowBanner = true;
    }

    public void HideBanner()
    {
        pendingShowBanner = false;
        bannerView?.Hide();
        isBannerDisplayed = false;
    }

    public void DestroyBanner()
    {
        pendingShowBanner = false;
        bannerView?.Destroy();
        bannerView = null;
        isBannerLoaded = false;
        isBannerDisplayed = false;
    }

    public bool IsBannerLoaded() => isBannerLoaded;
    public bool IsBannerDisplayed() => isBannerDisplayed;

    #endregion

    #region Rewarded

    public void LoadRewardedAd()
    {
        RewardedAd.Load(rewardedUnitId, new AdRequest(), (ad, error) =>
        {
            if (error != null)
            {
                isRewardedLoaded = false;
                Debug.LogError($"[AdMobManager] 리워드 광고 로드 실패: {error.GetMessage()}");
                OnAdFailedToLoad?.Invoke();
                return;
            }

            rewardedAd = ad;
            isRewardedLoaded = true;

            rewardedAd.OnAdFullScreenContentClosed += OnRewardedClosed;
            rewardedAd.OnAdFullScreenContentFailed += OnRewardedShowFailed;
        });
    }

    private void OnRewardedClosed()
    {
        isRewardedLoaded = false;
        OnAdClosed?.Invoke();
        LoadRewardedAd();
    }

    private void OnRewardedShowFailed(AdError error)
    {
        isRewardedLoaded = false;
        Debug.LogError($"[AdMobManager] 리워드 광고 표시 실패: {error.GetMessage()}");
        OnAdFailedToShow?.Invoke();
        LoadRewardedAd();
    }

    public void ShowRewardedAd()
    {
        if (rewardedAd == null || !isRewardedLoaded)
        {
            Debug.LogError("[AdMobManager] 리워드 광고 준비 안됨");
            OnAdFailedToShow?.Invoke();
            if (!isRewardedLoaded) LoadRewardedAd();
            return;
        }

        rewardedAd.Show(_ =>
        {
            Debug.Log("[AdMobManager] 리워드 지급");
            OnRewardEarned?.Invoke();
        });
        isRewardedLoaded = false;
    }

    public bool IsAdLoaded() => isRewardedLoaded;

    #endregion

    #region Interstitial

    private void LoadInterstitialAd()
    {
        if (string.IsNullOrEmpty(interstitialUnitId)) return;

        interstitialAd?.Destroy();
        interstitialAd = null;
        isInterstitialLoaded = false;

        InterstitialAd.Load(interstitialUnitId, new AdRequest(), (ad, error) =>
        {
            if (error != null)
            {
                Debug.LogError($"[AdMobManager] 전면 광고 로드 실패: {error.GetMessage()}");
                return;
            }

            interstitialAd = ad;
            isInterstitialLoaded = true;

            interstitialAd.OnAdFullScreenContentClosed += OnInterstitialClosed;
            interstitialAd.OnAdFullScreenContentFailed += OnInterstitialFailed;
        });
    }

    private void OnInterstitialClosed()
    {
        isInterstitialLoaded = false;
        onInterstitialClosed?.Invoke();
        onInterstitialClosed = null;
        LoadInterstitialAd();
    }

    private void OnInterstitialFailed(AdError error)
    {
        isInterstitialLoaded = false;
        Debug.LogError($"[AdMobManager] 전면 광고 표시 실패: {error.GetMessage()}");
        onInterstitialClosed?.Invoke();
        onInterstitialClosed = null;
        LoadInterstitialAd();
    }

    public void ShowInterstitialAd(Action onClosed = null)
    {
#if UNITY_EDITOR
        onClosed?.Invoke();
        return;
#endif
        onInterstitialClosed = onClosed;

        if (interstitialAd != null && isInterstitialLoaded && interstitialAd.CanShowAd())
        {
            interstitialAd.Show();
        }
        else
        {
            Debug.LogWarning("[AdMobManager] 전면 광고 준비 안됨 - 콜백 즉시 실행");
            onInterstitialClosed?.Invoke();
            onInterstitialClosed = null;
            LoadInterstitialAd();
        }
    }

    #endregion

    public void ClearAllListeners()
    {
        OnRewardEarned = null;
        OnAdClosed = null;
        OnAdFailedToLoad = null;
        OnAdFailedToShow = null;
    }

    private void OnDestroy()
    {
        bannerView?.Destroy();
        rewardedAd?.Destroy();
        interstitialAd?.Destroy();
    }
}