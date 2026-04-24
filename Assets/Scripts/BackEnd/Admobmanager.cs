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

        Debug.Log($"[AdMobManager] Initialize - bannerId:{bannerId} rewardedId:{rewardedId}");

        MobileAds.RaiseAdEventsOnUnityMainThread = true;

        MobileAds.Initialize(status =>
        {
            isInitialized = true;
            Debug.Log($"[AdMobManager] MobileAds 초기화 완료");

            foreach (var adapter in status.getAdapterStatusMap())
                Debug.Log($"[AdMobManager] Adapter: {adapter.Key} / {adapter.Value.InitializationState}");

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
        Debug.Log($"[AdMobManager] LoadBannerAd - bannerUnitId:{bannerUnitId}");
        bannerView?.Destroy();
        bannerView = new BannerView(bannerUnitId, AdSize.Banner, AdPosition.Bottom);

        bannerView.OnBannerAdLoaded += OnBannerLoaded;
        bannerView.OnBannerAdLoadFailed += OnBannerLoadFailed;
        bannerView.OnAdImpressionRecorded += OnBannerDisplayed;

        bannerView.LoadAd(new AdRequest());
    }

    private void OnBannerLoaded()
    {
        Debug.Log("[AdMobManager] 배너 로드 완료");
        isBannerLoaded = true;
        if (pendingShowBanner)
        {
            pendingShowBanner = false;
            Debug.Log("[AdMobManager] pendingShowBanner - 배너 표시");
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
        Debug.Log("[AdMobManager] 배너 표시됨");
        isBannerDisplayed = true;
    }

    public void ShowBanner()
    {
        Debug.Log($"[AdMobManager] ShowBanner 호출 - isInitialized:{isInitialized} isBannerLoaded:{isBannerLoaded} isBannerDisplayed:{isBannerDisplayed} bannerView:{bannerView != null}");
        if (!isInitialized) { pendingShowBannerOnInit = true; return; }
        if (bannerView == null) { LoadBannerAd(); pendingShowBanner = true; return; }
        if (isBannerLoaded) bannerView.Show();
        else { pendingShowBanner = true; }
    }

    public void HideBanner()
    {
        Debug.Log("[AdMobManager] HideBanner 호출");
        pendingShowBanner = false;
        bannerView?.Hide();
        isBannerDisplayed = false;
    }

    public void DestroyBanner()
    {
        Debug.Log("[AdMobManager] DestroyBanner 호출");
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
        Debug.Log($"[AdMobManager] LoadRewardedAd - rewardedUnitId:{rewardedUnitId}");
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
            Debug.Log("[AdMobManager] 리워드 광고 로드 완료");

            rewardedAd.OnAdFullScreenContentClosed += OnRewardedClosed;
            rewardedAd.OnAdFullScreenContentFailed += OnRewardedShowFailed;
        });
    }

    private void OnRewardedClosed()
    {
        Debug.Log("[AdMobManager] 리워드 광고 닫힘");
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
        Debug.Log($"[AdMobManager] ShowRewardedAd 호출 - isRewardedLoaded:{isRewardedLoaded} rewardedAd:{rewardedAd != null}");
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