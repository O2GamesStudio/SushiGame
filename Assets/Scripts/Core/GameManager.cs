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

    [Header("Panels")]
    [SerializeField] private WinPanel winPanel;
    [SerializeField] private LosePanel losePanel;
    [SerializeField] private GameObject addStaminaPanel;

    [Header("Add Time Panel")]
    [SerializeField] private GameObject addTimePanel;
    [SerializeField] private Button addTimeAdButton;
    [SerializeField] private Button addTimeCancelButton;

    [Header("Event Skip Panel")]
    [SerializeField] private GameObject eventSkipAdPanel;
    [SerializeField] private Button eventSkipAdButton;
    [SerializeField] private Button eventSkipLoseButton;

    private bool isGameWinProcessed = false;

    #region State

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

#if UNITY_EDITOR
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            SceneLoader.ReloadGame();
        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
            OnGameWin();
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            OnApplicationPause(true);
#endif

        if (!isGameActive || !isTimerStarted || isTimerFrozen) return;

        timeRemaining -= Time.deltaTime;
        gameUI.UpdateTimer(timeRemaining);

        if (timeRemaining <= 0)
            OnGameLose();
    }

    #region Init

    private void BindButtons()
    {
        addTimeAdButton?.onClick.AddListener(OnAddTimeAdButtonClicked);
        addTimeCancelButton?.onClick.AddListener(OnAddTimeCancelButtonClicked);
        eventSkipAdButton?.onClick.AddListener(OnEventSkipAdButtonClicked);
        eventSkipLoseButton?.onClick.AddListener(OnEventSkipLoseButtonClicked);
    }
    public void ResumeTimer()
    {
        if (this == null) return;
        if (freezeCoroutine != null)
        {
            StopCoroutine(freezeCoroutine);
            freezeCoroutine = null;
        }
        isTimerFrozen = false;
        gameUI.SetTimerFrozen(false);
        doorTransition?.PlayOpenAnimation();
    }
    private void StartGame()
    {
        isGameWinProcessed = false;
        var saveData = GameDataTransfer.Instance?.CurrentSaveData;
        if (saveData != null)
        {
            ResumeGame(saveData);
            return;
        }

        var levelGenerator = new LevelGenerator(currentLevel);
        var plateDataList = levelGenerator.GeneratePlates();
        var railData = levelGenerator.GetRailData();


        plateManager.Initialize(plateDataList, currentLevel.sequentialActivation, railData);
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
            TutorialManager.Instance?.TryShowTutorial(userData.currentStage);
        }
        if (userData.currentStage == 1 && saveData == null)
            InGameTutorialController.Instance?.StartTutorial();

        doorTransition?.PlayOpenAnimation();
        UnityAdsManager.Instance?.ShowBanner();
    }

    private void ResumeGame(GameSaveData saveData)
    {
        isGameWinProcessed = false;
        plateManager.RestoreFromSaveData(saveData, currentLevel);
        GameStateChecker.Instance.Initialize(plateManager);

        totalSushiSets = currentLevel.totalSushiSetCount;
        mergedSetsCount = saveData.mergedSetsCount;
        timeRemaining = saveData.timeRemaining;
        isGameActive = true;
        isTimerFrozen = false;
        isTimerStarted = false;

        GameDataTransfer.Instance.ClearSaveData();

        gameUI.ShowGame();
        gameUI.UpdateTimer(timeRemaining);
        gameUI.UpdateProgress(mergedSetsCount, totalSushiSets);

        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData != null)
        {
            ItemManager.Instance?.InitializeItemCounts(userData);
            gameUI.UpdateStage(userData.currentStage);
        }

        doorTransition?.PlayOpenAnimation();
        UnityAdsManager.Instance?.ShowBanner();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus || !isGameActive) return;

        int stageIndex = GameDataTransfer.Instance?.CurrentUserData?.currentStage ?? 0;
        var saveData = plateManager.GetSaveData(stageIndex, timeRemaining, mergedSetsCount);

        // 로컬 저장은 동기 유지 (빠름)
        GameSaveService.Instance?.SaveLocal(saveData);

        // Firestore는 백그라운드로
        System.Threading.Tasks.Task.Run(() =>
            GameSaveService.Instance?.SaveToFirestore(saveData));
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

        int stageIndex = GameDataTransfer.Instance?.CurrentUserData?.currentStage ?? 0;
        var saveData = plateManager.GetSaveData(stageIndex, timeRemaining, mergedSetsCount);
        GameSaveService.Instance?.OnMerged(saveData);
        InGameTutorialController.Instance?.OnSushiMerged(mergedTypeId);

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
        if (isGameWinProcessed) return;
        isGameWinProcessed = true;

        if (MergeEventSystem.Instance != null && MergeEventSystem.Instance.IsEventActive)
        {
            OnGameLose(true);
            return;
        }

        GameSaveService.Instance?.ClearLocal();
        GameSaveService.Instance?.ClearFirestore();
        GameDataTransfer.Instance?.SetLastMergedCount(mergedSetsCount);

        isGameActive = false;
        if (inputHandler != null) inputHandler.enabled = false;

        SoundManager.Instance?.PlayWinSFX();
        UnityAdsManager.Instance?.HideBanner();

        NetworkChecker.Instance?.Check(() =>
        {
            OnStageClear();
            winPanel?.SetLevelDataBase(levelDataBase);
            winPanel?.Show();
        });
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
        GameSaveService.Instance?.ClearLocal();
        GameSaveService.Instance?.ClearFirestore();
        GameDataTransfer.Instance?.SetLastMergedCount(mergedSetsCount);

        isGameActive = false;
        if (inputHandler != null) inputHandler.enabled = false;

        SoundManager.Instance?.PlayLoseSFX();
        gameUI.ShowLose();
        gameUI.SetTimerText("영업종료");
        UnityAdsManager.Instance?.HideBanner();

        NetworkChecker.Instance?.Check(() =>
        {
            ConsumeStamina(() =>
            {
                losePanel?.Show();
            });
        });
    }

    public void RefreshStaminaUI(UserData userData)
    {
        winPanel?.RefreshStaminaUI(userData);
        losePanel?.RefreshStaminaUI(userData);
    }

    #endregion

    #region Stage Clear

    private void OnStageClear()
    {
        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;

        int nextStage = userData.currentStage + 1;
        userData.currentStage = nextStage;
        GameDataTransfer.Instance.SetUserData(userData);

        string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (!string.IsNullOrEmpty(userId))
            UserDataService.Instance?.UpdateStage(userId, nextStage);
    }

    #endregion

    #region Ad Handling

    private void OnAddTimeAdButtonClicked()
    {
        bool isAdsRemoved = GameDataTransfer.Instance?.CurrentUserData?.isAdsRemoved ?? false;

        if (isAdsRemoved || UnityAdsManager.Instance == null)
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

        if (plateManager.AreAllPlatesEmpty())
            OnGameWin();
        else
        {
            isGameActive = true;
            if (inputHandler != null) inputHandler.enabled = true;
        }
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
    public void ShowAddStaminaPanel()
    {
        addStaminaPanel?.SetActive(true);
    }

    #endregion

    #region Lifecycle

    private void OnDestroy()
    {
        Instance = null;

        if (UnityAdsManager.Instance == null) return;
        UnityAdsManager.Instance.OnRewardEarned -= OnAddTimeAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow -= OnAddTimeAdFailed;
        UnityAdsManager.Instance.OnRewardEarned -= OnEventSkipAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow -= OnEventSkipAdFailed;
    }

    #endregion
}