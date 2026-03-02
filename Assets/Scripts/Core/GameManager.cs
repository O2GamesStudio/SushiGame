using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private LevelData currentLevel;
    [SerializeField] private PlateManager plateManager;
    [SerializeField] private GameUI gameUI;
    [SerializeField] private DoorTransition doorTransition;
    [SerializeField] private UnityEngine.UI.Button lobbyButton;
    [SerializeField] private UnityEngine.UI.Button restartButton;

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

        restartButton?.onClick.AddListener(() => SceneLoader.ReloadGame());

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

        totalSushiSets = currentLevel.totalSushiCount / 3;
        mergedSetsCount = 0;

        timeRemaining = currentLevel.timeLimitSeconds;
        isGameActive = true;
        isTimerFrozen = false;
        isTimerStarted = false;

        gameUI.ShowGame();
        gameUI.UpdateTimer(timeRemaining);
        gameUI.UpdateProgress(mergedSetsCount, totalSushiSets);

        MergeEventSystem.Instance?.Initialize(currentLevel.mergeEvents, currentLevel.specialPlateCount);

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

        yield return new WaitForSeconds(duration);

        isTimerFrozen = false;
        gameUI.SetTimerFrozen(false);
        freezeCoroutine = null;
    }

    public void OnGameWin()
    {
        isGameActive = false;
        gameUI.ShowWin();
        OnStageClear();
    }

    public void OnGameLose()
    {
        isGameActive = false;
        gameUI.ShowLose();
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