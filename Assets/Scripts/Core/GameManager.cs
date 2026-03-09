using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Core")]
    [SerializeField] private LevelData currentLevel;
    [SerializeField] private LevelDataBase levelDataBase;
    [SerializeField] private PlateManager plateManager;
    [SerializeField] private InputHandler inputHandler;
    [SerializeField] private GameUI gameUI;
    [SerializeField] private DoorTransition doorTransition;

    [Header("Buttons")]
    [SerializeField] private Button lobbyButton;
    [SerializeField] private Button loseLobbyButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button coinButton;
    [SerializeField] private Button coin2xButton;

    [Header("Win Panel")]
    [SerializeField] private TextMeshProUGUI winStaminaText;
    [SerializeField] private TextMeshProUGUI winStaminaChargingText;
    [SerializeField] private TextMeshProUGUI winCoinText;
    [SerializeField] private Button winStaminaButton;

    [Header("Lose Panel")]
    [SerializeField] private TextMeshProUGUI loseStaminaText;
    [SerializeField] private TextMeshProUGUI loseStaminaChargingText;
    [SerializeField] private TextMeshProUGUI loseCoinText;
    [SerializeField] private Button loseStaminaButton;
    [SerializeField] private GameObject addStaminaPanel;

    [Header("Add Time Panel")]
    [SerializeField] private GameObject addTimePanel;
    [SerializeField] private Button addTimeAdButton;
    [SerializeField] private Button addTimeCancelButton;

    [Header("Event Skip Panel")]
    [SerializeField] private GameObject eventSkipAdPanel;
    [SerializeField] private Button eventSkipAdButton;
    [SerializeField] private Button eventSkipLoseButton;

    #region State

    private bool isStageClearProcessed = false;
    private bool addTimePanelUsed = false;
    private int totalSushiSets;
    private int mergedSetsCount;

    public int TotalSets => totalSushiSets;
    public int MergedSets => mergedSetsCount;

    #endregion

    #region Timer State

    private float timeRemaining;
    private bool isGameActive;
    private bool isTimerFrozen;
    private bool isTimerStarted;
    private Coroutine freezeCoroutine;

    public bool IsTimerStarted => isTimerStarted;

    #endregion

    #region Stamina State

    private Coroutine staminaChargeCoroutine;

    #endregion

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        var transferData = GameDataTransfer.Instance?.CurrentLevelData;
        if (transferData != null)
            currentLevel = transferData;

        BindButtons();
        StartGame();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            SceneLoader.ReloadGame();

        if (!isGameActive || !isTimerStarted || isTimerFrozen) return;

        timeRemaining -= Time.deltaTime;
        gameUI.UpdateTimer(timeRemaining);

        if (timeRemaining <= 0)
            OnGameLose();
    }

    #region Init

    private void BindButtons()
    {
        retryButton?.onClick.AddListener(OnRetryButtonClicked);
        coinButton?.onClick.AddListener(() => ClaimCoinAndNextStage(100));
        coin2xButton?.onClick.AddListener(OnCoin2xButtonClicked);
        winStaminaButton?.onClick.AddListener(() => addStaminaPanel?.SetActive(true));
        loseStaminaButton?.onClick.AddListener(() => addStaminaPanel?.SetActive(true));
        addTimeAdButton?.onClick.AddListener(OnAddTimeAdButtonClicked);
        addTimeCancelButton?.onClick.AddListener(OnAddTimeCancelButtonClicked);
        eventSkipAdButton?.onClick.AddListener(OnEventSkipAdButtonClicked);
        eventSkipLoseButton?.onClick.AddListener(OnEventSkipLoseButtonClicked);

        lobbyButton?.onClick.AddListener(OnLobbyButtonClicked);
        loseLobbyButton?.onClick.AddListener(() => SceneLoader.LoadLobby());
    }

    private void StartGame()
    {
        var levelGenerator = new LevelGenerator(currentLevel);
        var plateDataList = levelGenerator.GeneratePlates();

        plateManager.Initialize(plateDataList, currentLevel.sequentialActivation);
        GameStateChecker.Instance.Initialize(plateManager);

        totalSushiSets = currentLevel.totalSushiSetCount;
        mergedSetsCount = 0;

        timeRemaining = currentLevel.timeLimitSeconds;
        isGameActive = true;
        isTimerFrozen = false;
        isTimerStarted = false;

        gameUI.ShowGame();
        gameUI.UpdateTimer(timeRemaining);
        gameUI.UpdateProgress(mergedSetsCount, totalSushiSets);

        MergeEventSystem.Instance?.Initialize(currentLevel.mergeEvents, currentLevel.specialPlateCount);

        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData != null)
        {
            ItemManager.Instance?.InitializeItemCounts(userData);
            gameUI.UpdateStage(userData.currentStage);
        }

        doorTransition?.PlayOpenAnimation();
        UnityAdsManager.Instance?.ShowBanner();
    }

    #endregion

    #region Timer

    public void StartTimer()
    {
        if (!isTimerStarted && isGameActive)
            isTimerStarted = true;
    }

    public void FreezeTimer(float duration)
    {
        if (freezeCoroutine != null)
            StopCoroutine(freezeCoroutine);
        freezeCoroutine = StartCoroutine(FreezeTimerCoroutine(duration));
    }

    private IEnumerator FreezeTimerCoroutine(float duration)
    {
        isTimerFrozen = true;
        gameUI.SetTimerFrozen(true);
        doorTransition?.PlayCloseAnimation();

        yield return new WaitForSeconds(duration);

        isTimerFrozen = false;
        gameUI.SetTimerFrozen(false);
        doorTransition?.PlayOpenAnimation();
        freezeCoroutine = null;
    }

    #endregion

    #region Merge

    public void OnSushiMerged(int mergedTypeId = -1, Plate plate = null)
    {
        mergedSetsCount++;
        gameUI.UpdateProgress(mergedSetsCount, totalSushiSets);

        if (MergeEventSystem.Instance == null) return;

        if (MergeEventSystem.Instance.IsEventActive)
            MergeEventSystem.Instance.OnSushiMergedDuringEvent(mergedTypeId, plate);
        else
            MergeEventSystem.Instance.OnSushiMerged(mergedSetsCount);
    }

    #endregion

    #region Win / Lose Flow

    public void OnGameWin()
    {
        if (MergeEventSystem.Instance != null && MergeEventSystem.Instance.IsEventActive)
        {
            OnGameLose(true);
            return;
        }

        isGameActive = false;
        if (inputHandler != null) inputHandler.enabled = false;
        gameUI.ShowWin();
        UnityAdsManager.Instance?.HideBanner();

        NetworkChecker.Instance?.Check(() => OnStageClear());

        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData != null)
        {
            if (winStaminaText != null) winStaminaText.text = userData.stamina.ToString();
            if (winCoinText != null) winCoinText.text = userData.coin.ToString();
            StartStaminaChargeDisplay(winStaminaChargingText, userData);
        }
    }

    public void OnGameLose(bool isEventFail = false)
    {
        isGameActive = false;
        if (inputHandler != null) inputHandler.enabled = false;

        if (isEventFail)
        {
            eventSkipAdPanel?.SetActive(true);
            return;
        }

        if (!addTimePanelUsed)
        {
            addTimePanelUsed = true;
            gameUI.SetTimerText("영업종료");
            addTimePanel?.SetActive(true);
            return;
        }

        ShowLoseResult();
    }

    private void ShowLoseResult()
    {
        isGameActive = false;
        if (inputHandler != null) inputHandler.enabled = false;
        gameUI.ShowLose();
        gameUI.SetTimerText("영업종료");
        UnityAdsManager.Instance?.HideBanner();

        NetworkChecker.Instance?.Check(() =>
        {
            ConsumeStamina(() =>
            {
                var userData = GameDataTransfer.Instance?.CurrentUserData;
                if (userData != null)
                {
                    if (loseStaminaText != null) loseStaminaText.text = userData.stamina.ToString();
                    if (loseCoinText != null) loseCoinText.text = userData.coin.ToString();
                    StartStaminaChargeDisplay(loseStaminaChargingText, userData);
                }
            });
        });
    }

    private void OnRetryButtonClicked()
    {
        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;

        if (userData.stamina < 1)
        {
            addStaminaPanel?.SetActive(true);
            return;
        }

        SceneLoader.LoadGameAsync(LoadingUI.Instance);
    }

    private void OnLobbyButtonClicked()
    {
        string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (!string.IsNullOrEmpty(userId) && isStageClearProcessed)
        {
            var userData = GameDataTransfer.Instance?.CurrentUserData;
            if (userData != null)
            {
                userData.coin += 100;
                GameDataTransfer.Instance.SetUserData(userData);
                UserDataService.Instance?.UpdateCurrency(userId, userData.stamina, userData.coin);
            }

            bool isUpdateComplete = false;
            UserDataService.Instance?.UpdateStage(
                userId,
                GameDataTransfer.Instance.CurrentUserData.currentStage,
                () => isUpdateComplete = true,
                (error) => isUpdateComplete = true
            );

            SceneLoader.LoadLobby(() => isUpdateComplete);
        }
        else
        {
            SceneLoader.LoadLobby();
        }
    }

    #endregion

    #region Stage Clear

    private void OnStageClear()
    {
        if (isStageClearProcessed) return;
        isStageClearProcessed = true;

        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;

        int nextStage = userData.currentStage + 1;
        userData.currentStage = nextStage;
        GameDataTransfer.Instance.SetUserData(userData);

        string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (!string.IsNullOrEmpty(userId))
            UserDataService.Instance?.UpdateStage(userId, nextStage);
    }

    private void LoadNextStage()
    {
        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;

        var nextLevelData = levelDataBase.Get(userData.currentStage);
        if (nextLevelData == null)
        {
            SceneLoader.LoadLobby();
            return;
        }

        GameDataTransfer.Instance.SetLevelData(nextLevelData);
        SceneLoader.LoadGameAsync(LoadingUI.Instance);
    }

    #endregion

    #region Coin / Reward

    private void ClaimCoinAndNextStage(int coinAmount)
    {
        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;

        string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId)) return;

        userData.coin += coinAmount;
        GameDataTransfer.Instance.SetUserData(userData);

        UserDataService.Instance?.UpdateCurrency(userId, userData.stamina, userData.coin, () =>
        {
            LoadNextStage();
        });
    }

    #endregion

    #region Ad Handling

    private void OnCoin2xButtonClicked()
    {
        if (UnityAdsManager.Instance == null) return;
        UnityAdsManager.Instance.OnRewardEarned += OnCoin2xAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow += OnCoin2xAdFailed;
        UnityAdsManager.Instance.ShowRewardedAd();
    }

    private void OnCoin2xAdRewardEarned()
    {
        UnityAdsManager.Instance.OnRewardEarned -= OnCoin2xAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow -= OnCoin2xAdFailed;
        ClaimCoinAndNextStage(200);
    }

    private void OnCoin2xAdFailed()
    {
        UnityAdsManager.Instance.OnRewardEarned -= OnCoin2xAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow -= OnCoin2xAdFailed;
    }

    private void OnAddTimeAdButtonClicked()
    {
        if (UnityAdsManager.Instance == null)
        {
            addTimePanel?.SetActive(false);
            ResumeGameAfterAddTime();
            return;
        }

        UnityAdsManager.Instance.OnRewardEarned += OnAddTimeAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow += OnAddTimeAdFailed;
        UnityAdsManager.Instance.ShowRewardedAd();
    }

    private void OnAddTimeAdRewardEarned()
    {
        UnityAdsManager.Instance.OnRewardEarned -= OnAddTimeAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow -= OnAddTimeAdFailed;
        addTimePanel?.SetActive(false);
        ResumeGameAfterAddTime();
    }

    private void OnAddTimeAdFailed()
    {
        UnityAdsManager.Instance.OnRewardEarned -= OnAddTimeAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow -= OnAddTimeAdFailed;
    }

    private void ResumeGameAfterAddTime()
    {
        timeRemaining = 30f;
        isGameActive = true;
        isTimerStarted = true;
        if (inputHandler != null) inputHandler.enabled = true;
        gameUI.SetTimerFrozen(false);
    }

    private void OnAddTimeCancelButtonClicked()
    {
        addTimePanel?.SetActive(false);
        ShowLoseResult();
    }

    private void OnEventSkipAdButtonClicked()
    {
        if (UnityAdsManager.Instance == null)
        {
            eventSkipAdPanel?.SetActive(false);
            OnEventSkipAdRewardEarned();
            return;
        }

        UnityAdsManager.Instance.OnRewardEarned += OnEventSkipAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow += OnEventSkipAdFailed;
        UnityAdsManager.Instance.ShowRewardedAd();
    }

    private void OnEventSkipAdRewardEarned()
    {
        UnityAdsManager.Instance.OnRewardEarned -= OnEventSkipAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow -= OnEventSkipAdFailed;
        eventSkipAdPanel?.SetActive(false);
        MergeEventSystem.Instance?.ForceCompleteEvent();
        isGameActive = true;
        if (inputHandler != null) inputHandler.enabled = true;
    }

    private void OnEventSkipAdFailed()
    {
        UnityAdsManager.Instance.OnRewardEarned -= OnEventSkipAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow -= OnEventSkipAdFailed;
    }

    private void OnEventSkipLoseButtonClicked()
    {
        eventSkipAdPanel?.SetActive(false);
        ShowLoseResult();
    }

    #endregion

    #region Stamina

    private void ConsumeStamina(Action onComplete = null)
    {
        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) { onComplete?.Invoke(); return; }

        string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId)) { onComplete?.Invoke(); return; }

        bool wasFullBefore = userData.stamina >= StaminaChargeCalculator.MaxStamina;

        userData.stamina = Mathf.Max(0, userData.stamina - 1);

        if (wasFullBefore)
            userData.staminaLastChargeTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        GameDataTransfer.Instance.SetUserData(userData);
        UserDataService.Instance?.UpdateStaminaData(userId, userData.stamina, userData.staminaLastChargeTime, onComplete);
    }

    private void StartStaminaChargeDisplay(TextMeshProUGUI chargingText, UserData userData)
    {
        if (chargingText == null) return;

        if (userData.stamina >= StaminaChargeCalculator.MaxStamina)
        {
            chargingText.gameObject.SetActive(false);
            return;
        }

        var (_, _, remainingSeconds) = StaminaChargeCalculator.Calculate(userData.stamina, userData.staminaLastChargeTime);
        chargingText.gameObject.SetActive(true);

        if (staminaChargeCoroutine != null)
            StopCoroutine(staminaChargeCoroutine);
        staminaChargeCoroutine = StartCoroutine(StaminaChargeCoroutine(chargingText, remainingSeconds));
    }

    private IEnumerator StaminaChargeCoroutine(TextMeshProUGUI chargingText, float remainingSeconds)
    {
        float timeLeft = remainingSeconds;

        while (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            int s = Mathf.Max(0, Mathf.CeilToInt(timeLeft));
            if (chargingText != null)
                chargingText.text = $"{s / 60}m {s % 60}s";
            yield return null;
        }
    }

    public void RefreshStaminaUI(UserData userData)
    {
        if (winStaminaText != null) winStaminaText.text = userData.stamina.ToString();
        if (loseStaminaText != null) loseStaminaText.text = userData.stamina.ToString();
    }

    #endregion

    #region Lifecycle

    private void OnDestroy()
    {
        if (UnityAdsManager.Instance == null) return;
        UnityAdsManager.Instance.OnRewardEarned -= OnCoin2xAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow -= OnCoin2xAdFailed;
        UnityAdsManager.Instance.OnRewardEarned -= OnAddTimeAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow -= OnAddTimeAdFailed;
        UnityAdsManager.Instance.OnRewardEarned -= OnEventSkipAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow -= OnEventSkipAdFailed;
    }

    #endregion
}