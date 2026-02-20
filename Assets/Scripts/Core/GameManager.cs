using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private LevelData currentLevel;
    [SerializeField] private PlateManager plateManager;
    [SerializeField] private GameUI gameUI;
    [SerializeField] private DoorTransition doorTransition;

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
        StartGame();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            RestartGame();
        }

        if (!isGameActive || !isTimerStarted) return;

        if (!isTimerFrozen)
        {
            timeRemaining -= Time.deltaTime;
            gameUI.UpdateTimer(timeRemaining);

            if (timeRemaining <= 0)
            {
                OnGameLose();
            }
        }
    }
    private void StartGame()
    {
        levelGenerator = new LevelGenerator(currentLevel);
        var plateDataList = levelGenerator.GeneratePlates();

        plateManager.Initialize(plateDataList);
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

        if (doorTransition != null)
        {
            doorTransition.PlayOpenAnimation();
        }
    }

    public void OnSushiMerged()
    {
        mergedSetsCount++;
        gameUI.UpdateProgress(mergedSetsCount, totalSushiSets);
    }

    public void StartTimer()
    {
        if (!isTimerStarted && isGameActive)
        {
            isTimerStarted = true;
        }
    }

    public void FreezeTimer(float duration)
    {
        if (freezeCoroutine != null)
        {
            StopCoroutine(freezeCoroutine);
        }
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
    }

    public void OnGameLose()
    {
        isGameActive = false;
        gameUI.ShowLose();
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}