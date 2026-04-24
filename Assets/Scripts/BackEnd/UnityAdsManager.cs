using UnityEngine;
using Unity.Services.Core;
using Unity.Services.LevelPlay;
using System;
using System.Collections;

public class UnityAdsManager : MonoBehaviour
{
    private static UnityAdsManager instance;
    public static UnityAdsManager Instance
    {
        get
        {
            if (isQuitting) return null;
            if (instance == null)
            {
                GameObject go = new GameObject("UnityAdsManager");
                instance = go.AddComponent<UnityAdsManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    [SerializeField] private AdsConfig adsConfig;

    private string rewardedAdUnitId;
    private string bannerAdUnitId;

    private LevelPlayRewardedAd rewardedAd;
    private LevelPlayBannerAd bannerAd;

    private bool isInitialized = false;
    private bool isAdLoaded = false;
    private bool isLoadingAd = false;
    private bool isBannerLoaded = false;
    private bool isBannerDisplayed = false;
    private bool pendingShowBanner = false;

    private Coroutine bannerRetryCoroutine;
    private Coroutine bannerInitWaitCoroutine;
    private Coroutine rewardedRetryCoroutine;

    public event Action OnRewardEarned;
    public event Action OnAdClosed;
    public event Action OnAdFailedToLoad;
    public event Action OnAdFailedToShow;

    private static bool isQuitting = false;

    private bool UseAdMob => adsConfig != null && adsConfig.useAdMob;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            if (adsConfig == null) return;

#if UNITY_ANDROID
            rewardedAdUnitId = adsConfig.androidRewardedAdUnitId;
            bannerAdUnitId = adsConfig.androidBannerAdUnitId;
#elif UNITY_IOS
            rewardedAdUnitId = adsConfig.iOSRewardedAdUnitId;
            bannerAdUnitId   = adsConfig.iOSBannerAdUnitId;
#else
            rewardedAdUnitId = adsConfig.androidRewardedAdUnitId;
            bannerAdUnitId   = adsConfig.androidBannerAdUnitId;
#endif
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (adsConfig == null) return;

        if (UseAdMob)
        {
            InitializeAdMob();
            return;
        }

        if (string.IsNullOrEmpty(adsConfig.appKey))
        {
            LogError("AdsConfig가 없거나 AppKey가 비어있습니다.");
            return;
        }

        StartCoroutine(InitializeLevelPlay());
    }

    #region AdMob Delegation

    private void InitializeAdMob()
    {
#if UNITY_ANDROID
        string admobBannerId = adsConfig.androidAdMobBannerUnitId;
        string admobRewardedId = adsConfig.androidAdMobRewardedUnitId;
#elif UNITY_IOS
        string admobBannerId   = adsConfig.iOSAdMobBannerUnitId;
        string admobRewardedId = adsConfig.iOSAdMobRewardedUnitId;
#else
        string admobBannerId   = adsConfig.androidAdMobBannerUnitId;
        string admobRewardedId = adsConfig.androidAdMobRewardedUnitId;
#endif
        var go = new GameObject("AdMobManager");
        DontDestroyOnLoad(go);
        var mgr = go.AddComponent<AdMobManager>();
        mgr.Initialize(admobBannerId, admobRewardedId);

        mgr.OnRewardEarned += () => OnRewardEarned?.Invoke();
        mgr.OnAdClosed += () => OnAdClosed?.Invoke();
        mgr.OnAdFailedToLoad += () => OnAdFailedToLoad?.Invoke();
        mgr.OnAdFailedToShow += () => OnAdFailedToShow?.Invoke();
    }

    #endregion

    private void Log(string message) => Debug.Log($"[UnityAdsManager] {message}");
    private void LogError(string message) => Debug.LogError($"[UnityAdsManager] {message}");

    #region LevelPlay Init

    private IEnumerator InitializeLevelPlay()
    {
        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;

        var initTask = UnityServices.InitializeAsync();
        while (!initTask.IsCompleted)
            yield return null;

        if (initTask.IsFaulted)
        {
            LogError($"Unity Services 초기화 실패 - {initTask.Exception?.Message}");
            yield break;
        }

        LevelPlay.Init(adsConfig.appKey);
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        Log("LevelPlay 초기화 완료");
        isInitialized = true;

        SetupRewardedAd();
        SetupBannerAd();
        LoadBannerAd();
        StartBannerRetry();
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        LogError($"LevelPlay 초기화 실패 - {error.ErrorMessage}");
        isInitialized = false;
    }

    private void StartBannerRetry()
    {
        if (bannerRetryCoroutine != null) StopCoroutine(bannerRetryCoroutine);
        bannerRetryCoroutine = StartCoroutine(BannerRetryRoutine());
    }

    private IEnumerator BannerRetryRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);
            if (!isBannerDisplayed && isInitialized)
            {
                isBannerLoaded = false;
                pendingShowBanner = true;
                LoadBannerAd();
            }
        }
    }

    #endregion

    #region Rewarded Ad (LevelPlay)

    private void SetupRewardedAd()
    {
        rewardedAd = new LevelPlayRewardedAd(rewardedAdUnitId);
        rewardedAd.OnAdLoaded += OnRewardedAdLoaded;
        rewardedAd.OnAdLoadFailed += OnRewardedAdLoadFailed;
        rewardedAd.OnAdDisplayed += OnRewardedAdDisplayed;
        rewardedAd.OnAdDisplayFailed += OnRewardedAdDisplayFailed;
        rewardedAd.OnAdClosed += OnRewardedAdClosedInternal;
        rewardedAd.OnAdRewarded += OnRewardedAdRewardedInternal;
        LoadRewardedAd();
    }

    public void LoadRewardedAd()
    {
        if (UseAdMob) { AdMobManager.Instance?.LoadRewardedAd(); return; }
        if (isLoadingAd) return;
        if (!isInitialized) { StartCoroutine(LoadAdWithDelay(3f)); return; }

        isLoadingAd = true;
        isAdLoaded = false;

        try { rewardedAd?.LoadAd(); }
        catch (Exception e)
        {
            isLoadingAd = false;
            LogError($"광고 로드 예외 - {e.Message}");
            OnAdFailedToLoad?.Invoke();
            ScheduleRewardedRetry(10f);
        }
    }

    private void ScheduleRewardedRetry(float delay)
    {
        if (rewardedRetryCoroutine != null) StopCoroutine(rewardedRetryCoroutine);
        rewardedRetryCoroutine = StartCoroutine(LoadAdWithDelay(delay));
    }

    private IEnumerator LoadAdWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        rewardedRetryCoroutine = null;
        LoadRewardedAd();
    }

    private void OnRewardedAdLoaded(LevelPlayAdInfo adInfo)
    {
        isLoadingAd = false;
        isAdLoaded = true;
    }

    private void OnRewardedAdLoadFailed(LevelPlayAdError error)
    {
        isLoadingAd = false;
        isAdLoaded = false;
        LogError($"광고 로드 실패 - {error.ErrorCode}: {error.ErrorMessage}");
        OnAdFailedToLoad?.Invoke();
        ScheduleRewardedRetry(10f);
    }

    private void OnRewardedAdDisplayed(LevelPlayAdInfo adInfo) { }

    private void OnRewardedAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        LogError($"광고 표시 실패 - {error.ErrorMessage}");
        isAdLoaded = false;
        OnAdFailedToShow?.Invoke();
        ScheduleRewardedRetry(0.5f);
    }

    private void OnRewardedAdClosedInternal(LevelPlayAdInfo adInfo)
    {
        OnAdClosed?.Invoke();
        ScheduleRewardedRetry(0.5f);
    }

    private void OnRewardedAdRewardedInternal(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        OnRewardEarned?.Invoke();
    }

    public void ShowRewardedAd()
    {
        if (UseAdMob) { AdMobManager.Instance?.ShowRewardedAd(); return; }

        if (!isInitialized) { LogError("LevelPlay 초기화되지 않음"); OnAdFailedToShow?.Invoke(); return; }

        if (rewardedAd != null && rewardedAd.IsAdReady())
        {
            try { rewardedAd.ShowAd(); isAdLoaded = false; }
            catch (Exception e)
            {
                LogError($"광고 표시 예외 - {e.Message}");
                isAdLoaded = false;
                OnAdFailedToShow?.Invoke();
                ScheduleRewardedRetry(0.5f);
            }
        }
        else
        {
            isAdLoaded = false;
            OnAdFailedToShow?.Invoke();
            if (!isLoadingAd) LoadRewardedAd();
        }
    }

    #endregion

    #region Banner Ad (LevelPlay)

    private void SetupBannerAd()
    {
        try
        {
            var configBuilder = new LevelPlayBannerAd.Config.Builder()
                .SetSize(LevelPlayAdSize.BANNER)
                .SetPosition(LevelPlayBannerPosition.BottomCenter)
                .SetDisplayOnLoad(false);

            bannerAd = new LevelPlayBannerAd(bannerAdUnitId, configBuilder.Build());
            bannerAd.OnAdLoaded += OnBannerAdLoaded;
            bannerAd.OnAdLoadFailed += OnBannerAdLoadFailed;
            bannerAd.OnAdDisplayed += OnBannerAdDisplayed;
            bannerAd.OnAdDisplayFailed += OnBannerAdDisplayFailed;
        }
        catch (Exception e) { LogError($"배너 설정 실패 - {e.Message}"); }
    }

    public void LoadBannerAd()
    {
        if (UseAdMob) return;

        if (!isInitialized)
        {
            if (bannerInitWaitCoroutine == null)
                bannerInitWaitCoroutine = StartCoroutine(LoadBannerAfterInit());
            return;
        }

        try { bannerAd?.LoadAd(); }
        catch (Exception e) { LogError($"배너 로드 예외 - {e.Message}"); }
    }

    private IEnumerator LoadBannerAfterInit()
    {
        float waitTime = 0f;
        while (!isInitialized && waitTime < 10f)
        {
            yield return new WaitForSeconds(0.5f);
            waitTime += 0.5f;
        }
        bannerInitWaitCoroutine = null;
        if (isInitialized) LoadBannerAd();
        else LogError("LevelPlay 초기화 타임아웃 - 배너 로드 실패");
    }

    private void OnBannerAdLoaded(LevelPlayAdInfo adInfo)
    {
        isBannerLoaded = true;
        if (pendingShowBanner)
        {
            pendingShowBanner = false;
            try { bannerAd?.ShowAd(); }
            catch (Exception e) { LogError($"배너 표시 예외 - {e.Message}"); }
        }
    }

    private void OnBannerAdLoadFailed(LevelPlayAdError error)
    {
        LogError($"배너 로드 실패 - {error.ErrorCode}: {error.ErrorMessage}");
        isBannerLoaded = false;
        isBannerDisplayed = false;
    }

    private void OnBannerAdDisplayed(LevelPlayAdInfo adInfo) => isBannerDisplayed = true;
    private void OnBannerAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        LogError($"배너 표시 실패 - {error.ErrorMessage}");
        isBannerDisplayed = false;
    }

    public void ShowBanner()
    {
        if (UseAdMob) { AdMobManager.Instance?.ShowBanner(); return; }

#if UNITY_EDITOR
        return;
#endif
        if (string.IsNullOrEmpty(bannerAdUnitId)) return;
        if (!isInitialized) { pendingShowBanner = true; return; }

        // retry 재시작
        if (bannerRetryCoroutine == null)
            StartBannerRetry();

        try
        {
            if (bannerAd == null)
            {
                SetupBannerAd();
                if (bannerAd == null) return;
                pendingShowBanner = true;
                LoadBannerAd();
                return;
            }

            if (isBannerLoaded) bannerAd?.ShowAd();
            else { pendingShowBanner = true; bannerAd?.LoadAd(); }
        }
        catch (Exception e) { LogError($"배너 표시 예외 - {e.Message}"); }
    }

    public void HideBanner()
    {
        if (UseAdMob) { AdMobManager.Instance?.HideBanner(); return; }

        pendingShowBanner = false;
        if (bannerRetryCoroutine != null)
        {
            StopCoroutine(bannerRetryCoroutine);
            bannerRetryCoroutine = null;
        }
        try { bannerAd?.HideAd(); isBannerDisplayed = false; }
        catch (Exception e) { LogError($"배너 숨김 예외 - {e.Message}"); }
    }

    public void DestroyBanner()
    {
        if (UseAdMob) { AdMobManager.Instance?.DestroyBanner(); return; }

        pendingShowBanner = false;
        try { bannerAd?.DestroyAd(); }
        catch (Exception e) { LogError($"배너 파괴 예외 - {e.Message}"); }
        bannerAd = null;
        isBannerLoaded = false;
        isBannerDisplayed = false;
    }

    #endregion

    #region Public Status

    public bool IsBannerLoaded() => UseAdMob ? (AdMobManager.Instance?.IsBannerLoaded() ?? false) : isBannerLoaded;
    public bool IsBannerDisplayed() => UseAdMob ? (AdMobManager.Instance?.IsBannerDisplayed() ?? false) : isBannerDisplayed;
    public bool IsAdLoaded() => UseAdMob ? (AdMobManager.Instance?.IsAdLoaded() ?? false) : (isAdLoaded && rewardedAd != null && rewardedAd.IsAdReady());
    public bool IsLoadingAd() => isLoadingAd;
    public bool IsInitialized() => UseAdMob ? AdMobManager.Instance != null : isInitialized;

    public void ClearAllListeners()
    {
        OnRewardEarned = null;
        OnAdClosed = null;
        OnAdFailedToLoad = null;
        OnAdFailedToShow = null;
        AdMobManager.Instance?.ClearAllListeners();
    }

    #endregion

    private void OnApplicationQuit() => isQuitting = true;

    private void OnDestroy()
    {
        if (bannerRetryCoroutine != null) StopCoroutine(bannerRetryCoroutine);
        if (bannerInitWaitCoroutine != null) StopCoroutine(bannerInitWaitCoroutine);
        if (rewardedRetryCoroutine != null) StopCoroutine(rewardedRetryCoroutine);

        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;

        if (rewardedAd != null)
        {
            rewardedAd.OnAdLoaded -= OnRewardedAdLoaded;
            rewardedAd.OnAdLoadFailed -= OnRewardedAdLoadFailed;
            rewardedAd.OnAdDisplayed -= OnRewardedAdDisplayed;
            rewardedAd.OnAdDisplayFailed -= OnRewardedAdDisplayFailed;
            rewardedAd.OnAdClosed -= OnRewardedAdClosedInternal;
            rewardedAd.OnAdRewarded -= OnRewardedAdRewardedInternal;
        }

        if (bannerAd != null)
        {
            bannerAd.OnAdLoaded -= OnBannerAdLoaded;
            bannerAd.OnAdLoadFailed -= OnBannerAdLoadFailed;
            bannerAd.OnAdDisplayed -= OnBannerAdDisplayed;
            bannerAd.OnAdDisplayFailed -= OnBannerAdDisplayFailed;
            bannerAd.DestroyAd();
        }
    }
}