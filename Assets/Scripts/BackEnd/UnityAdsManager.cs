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

    public event Action OnRewardEarned;
    public event Action OnAdClosed;
    public event Action OnAdFailedToLoad;
    public event Action OnAdFailedToShow;
    private static bool isQuitting = false;

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
            bannerAdUnitId = adsConfig.iOSBannerAdUnitId;
#else
            rewardedAdUnitId = adsConfig.androidRewardedAdUnitId;
            bannerAdUnitId = adsConfig.androidBannerAdUnitId;
#endif
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (adsConfig == null || string.IsNullOrEmpty(adsConfig.appKey))
        {
            LogError("AdsConfig가 없거나 AppKey가 비어있습니다.");
            return;
        }

        StartCoroutine(InitializeLevelPlay());
    }

    private void Log(string message) => Debug.Log($"[UnityAdsManager] {message}");
    private void LogError(string message) => Debug.LogError($"[UnityAdsManager] {message}");

    private IEnumerator InitializeLevelPlay()
    {
        // Fix 8: 중복 등록 방지
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
        // pendingShowBanner는 OnBannerAdLoaded에서 처리
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        LogError($"LevelPlay 초기화 실패 - {error.ErrorMessage}");
        isInitialized = false;
    }

    private void StartBannerRetry()
    {
        if (bannerRetryCoroutine != null)
            StopCoroutine(bannerRetryCoroutine);
        bannerRetryCoroutine = StartCoroutine(BannerRetryRoutine());
    }

    private IEnumerator BannerRetryRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(30f);

            if (!isBannerDisplayed && isInitialized)
            {
                isBannerLoaded = false;
                pendingShowBanner = true;
                LoadBannerAd();
            }
        }
    }

    #region 보상형 광고

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
        if (isLoadingAd) return;

        if (!isInitialized)
        {
            StartCoroutine(LoadAdWithDelay(3f));
            return;
        }

        isLoadingAd = true;
        isAdLoaded = false;

        try
        {
            rewardedAd?.LoadAd();
        }
        catch (Exception e)
        {
            isLoadingAd = false;
            LogError($"광고 로드 예외 - {e.Message}");
            OnAdFailedToLoad?.Invoke();
            StartCoroutine(LoadAdWithDelay(10f));
        }
    }

    private IEnumerator LoadAdWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadRewardedAd();
    }

    private void OnRewardedAdLoaded(LevelPlayAdInfo adInfo)
    {
        isLoadingAd = false;
        isAdLoaded = true;
        Log("보상형 광고 로드 완료");
    }

    private void OnRewardedAdLoadFailed(LevelPlayAdError error)
    {
        isLoadingAd = false;
        isAdLoaded = false;
        LogError($"광고 로드 실패 - {error.ErrorCode}: {error.ErrorMessage}");
        OnAdFailedToLoad?.Invoke();
        StartCoroutine(LoadAdWithDelay(10f));
    }

    private void OnRewardedAdDisplayed(LevelPlayAdInfo adInfo) => Log("보상형 광고 표시됨");

    private void OnRewardedAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        LogError($"광고 표시 실패 - {error.ErrorMessage}");
        isAdLoaded = false;
        OnAdFailedToShow?.Invoke();
        StartCoroutine(LoadAdWithDelay(0.5f));
    }

    private void OnRewardedAdClosedInternal(LevelPlayAdInfo adInfo)
    {
        Log("광고 닫힘");
        OnAdClosed?.Invoke();
        StartCoroutine(LoadAdWithDelay(0.5f));
    }

    private void OnRewardedAdRewardedInternal(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        Log("광고 시청 완료 - 보상 지급");
        OnRewardEarned?.Invoke();
    }

    public void ShowRewardedAd()
    {
        if (!isInitialized)
        {
            LogError("LevelPlay 초기화되지 않음");
            OnAdFailedToShow?.Invoke();
            return;
        }

        // Fix 3: isAdLoaded 대신 IsAdReady()만 체크
        if (rewardedAd != null && rewardedAd.IsAdReady())
        {
            try
            {
                rewardedAd.ShowAd();
                isAdLoaded = false;
            }
            catch (Exception e)
            {
                LogError($"광고 표시 예외 - {e.Message}");
                isAdLoaded = false;
                OnAdFailedToShow?.Invoke();
                StartCoroutine(LoadAdWithDelay(0.5f));
            }
        }
        else
        {
            isAdLoaded = false;
            OnAdFailedToShow?.Invoke();
            if (!isLoadingAd)
                LoadRewardedAd();
        }
    }

    #endregion

    #region 배너 광고

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
        catch (Exception e)
        {
            LogError($"배너 설정 실패 - {e.Message}");
        }
    }

    public void LoadBannerAd()
    {
        if (!isInitialized)
        {
            StartCoroutine(LoadBannerAfterInit());
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

        if (isInitialized)
            LoadBannerAd();
        else
            LogError("LevelPlay 초기화 타임아웃 - 배너 로드 실패");
    }

    private void OnBannerAdLoaded(LevelPlayAdInfo adInfo)
    {
        Log("배너 광고 로드 완료");
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
        // Fix 6: pendingShowBanner 유지 — 재시도 시 자동 표시 보장
    }

    private void OnBannerAdDisplayed(LevelPlayAdInfo adInfo)
    {
        Log("배너 광고 표시됨");
        isBannerDisplayed = true;
    }

    private void OnBannerAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        LogError($"배너 표시 실패 - {error.ErrorMessage}");
        isBannerDisplayed = false;
    }

    public void ShowBanner()
    {
        if (string.IsNullOrEmpty(bannerAdUnitId)) return;

        // Fix 2: 초기화 전 호출 시 pending 처리
        if (!isInitialized)
        {
            pendingShowBanner = true;
            return;
        }

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

            if (isBannerLoaded)
                bannerAd?.ShowAd();
            else
            {
                pendingShowBanner = true;
                bannerAd?.LoadAd();
            }
        }
        catch (Exception e)
        {
            LogError($"배너 표시 예외 - {e.Message}");
        }
    }

    public void HideBanner()
    {
        pendingShowBanner = false;
        try
        {
            bannerAd?.HideAd();
            isBannerDisplayed = false;
        }
        catch (Exception e)
        {
            LogError($"배너 숨김 예외 - {e.Message}");
        }
    }

    public void DestroyBanner()
    {
        pendingShowBanner = false;
        try
        {
            bannerAd?.DestroyAd();
        }
        catch (Exception e)
        {
            LogError($"배너 파괴 예외 - {e.Message}");
        }
        bannerAd = null;
        isBannerLoaded = false;
        isBannerDisplayed = false;
    }
    public bool IsBannerLoaded() => isBannerLoaded;
    public bool IsBannerDisplayed() => isBannerDisplayed;
    public bool IsAdLoaded() => isAdLoaded && rewardedAd != null && rewardedAd.IsAdReady();
    public bool IsLoadingAd() => isLoadingAd;
    public bool IsInitialized() => isInitialized;

    #endregion

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void OnDestroy()
    {
        if (bannerRetryCoroutine != null)
            StopCoroutine(bannerRetryCoroutine);

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