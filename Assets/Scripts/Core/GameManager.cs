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

    [Header("Lose Panel")]
    [SerializeField] private TextMeshProUGUI loseStaminaText;
    [SerializeField] private TextMeshProUGUI loseStaminaChargingText;
    [SerializeField] private TextMeshProUGUI loseCoinText;
    [SerializeField] private GameObject addStaminaPanel;

    private bool isStageClearProcessed = false;
    private int totalSushiSets;
    private int mergedSetsCount;

    public int TotalSets => totalSushiSets;
    public int MergedSets => mergedSetsCount;

    private LevelGenerator levelGenerator;
    private float timeRemaining;
    private bool isGameActive;
    private bool isTimerFrozen;
    private bool isTimerStarted;
    private Coroutine freezeCoroutine;
    private Coroutine staminaChargeCoroutine;

    public bool IsTimerStarted => isTimerStarted;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        var transferData = GameDataTransfer.Instance?.CurrentLevelData;
        if (transferData != null)
            currentLevel = transferData;

        retryButton?.onClick.AddListener(OnRetryButtonClicked);
        coinButton?.onClick.AddListener(() => ClaimCoinAndNextStage(100));
        coin2xButton?.onClick.AddListener(() => ClaimCoinAndNextStage(200));

        lobbyButton?.onClick.AddListener(() =>
        {
            string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
            if (!string.IsNullOrEmpty(userId) && isStageClearProcessed)
            {
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
        });

        loseLobbyButton?.onClick.AddListener(() => SceneLoader.LoadLobby());

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

    private void StartGame()
    {
        levelGenerator = new LevelGenerator(currentLevel);
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

        if (doorTransition != null)
            doorTransition.PlayOpenAnimation();
    }

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

    public void OnGameWin()
    {
        isGameActive = false;
        if (inputHandler != null) inputHandler.enabled = false;
        gameUI.ShowWin();
        OnStageClear();

        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData != null)
        {
            if (winStaminaText != null) winStaminaText.text = userData.stamina.ToString();
            if (winCoinText != null) winCoinText.text = userData.coin.ToString();
            StartStaminaChargeDisplay(winStaminaChargingText, userData);
        }
    }

    public void OnGameLose()
    {
        isGameActive = false;
        if (inputHandler != null) inputHandler.enabled = false;
        gameUI.ShowLose();
        gameUI.SetTimerText("영업종료");

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
    }

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
}