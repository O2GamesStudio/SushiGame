using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private LevelData currentLevel;
    [SerializeField] private PlateManager plateManager;
    [SerializeField] private GameUI gameUI;

    private LevelGenerator levelGenerator;
    private float timeRemaining;
    private bool isGameActive;
    private bool isTimerFrozen;
    private Coroutine freezeCoroutine;

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
        if (!isGameActive) return;

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

    private void StartGame()
    {
        levelGenerator = new LevelGenerator(currentLevel);
        var plateDataList = levelGenerator.GeneratePlates();

        plateManager.Initialize(plateDataList);
        GameStateChecker.Instance.Initialize(plateManager);

        timeRemaining = currentLevel.timeLimitSeconds;
        isGameActive = true;
        isTimerFrozen = false;

        gameUI.ShowGame();
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
        if (freezeCoroutine != null)
        {
            StopCoroutine(freezeCoroutine);
            freezeCoroutine = null;
        }
        isTimerFrozen = false;
        StartGame();
    }
}