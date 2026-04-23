using System;
using UnityEngine;
using GoogleMobileAds.Api;

public class AdMobManager : MonoBehaviour
{
    public static AdMobManager Instance { get; private set; }

    private string bannerUnitId;
    private string rewardedUnitId;

    private BannerView bannerView;
    private RewardedAd rewardedAd;

    private bool isBannerLoaded = false;
    private bool isBannerDisplayed = false;
    private bool isRewardedLoaded = false;
    private bool pendingShowBanner = false;
    private bool isInitialized = false;
    private bool pendingShowBannerOnInit = false;


    public event Action OnRewardEarned;
    public event Action OnAdClosed;
    public event Action OnAdFailedToLoad;
    public event Action OnAdFailedToShow;

    public void Initialize(string bannerId, string rewardedId)
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        bannerUnitId = bannerId;
        rewardedUnitId = rewardedId;

        MobileAds.Initialize(_ =>
        {
            isInitialized = true;
            LoadRewardedAd();
            LoadBannerAd();

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

    private void OnBannerDisplayed()
    {
        isBannerDisplayed = true;
    }

    public void ShowBanner()
    {
#if UNITY_EDITOR
        return;
#endif
        if (!isInitialized) { pendingShowBannerOnInit = true; return; }
        if (bannerView == null) { LoadBannerAd(); pendingShowBanner = true; return; }
        if (isBannerLoaded) bannerView.Show();
        else { pendingShowBanner = true; }
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
#if UNITY_EDITOR
        OnAdFailedToShow?.Invoke();
        return;
#endif
        if (rewardedAd == null || !isRewardedLoaded)
        {
            OnAdFailedToShow?.Invoke();
            if (!isRewardedLoaded) LoadRewardedAd();
            return;
        }

        rewardedAd.Show(_ => OnRewardEarned?.Invoke());
        isRewardedLoaded = false;
    }

    public bool IsAdLoaded() => isRewardedLoaded;

    public void ClearAllListeners()
    {
        OnRewardEarned = null;
        OnAdClosed = null;
        OnAdFailedToLoad = null;
        OnAdFailedToShow = null;
    }

    #endregion

    private void OnDestroy()
    {
        bannerView?.Destroy();
        rewardedAd?.Destroy();
    }
}