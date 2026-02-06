using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private LevelData currentLevel;
    [SerializeField] private PlateManager plateManager;
    [SerializeField] private GameUI gameUI;

    private LevelGenerator levelGenerator;
    private float timeRemaining;
    private bool isGameActive;

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

        timeRemaining -= Time.deltaTime;
        gameUI.UpdateTimer(timeRemaining);

        if (timeRemaining <= 0)
        {
            OnGameLose();
        }
    }

    private void StartGame()
    {
        levelGenerator = new LevelGenerator(currentLevel);
        var plateDataList = levelGenerator.GeneratePlates();
        
        plateManager.Initialize(plateDataList);
        GameStateChecker.Instance.Initialize(plateManager);

        timeRemaining = currentLevel.timeLimitSeconds;
        isGameActive = true;
        
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
        StartGame();
    }
}
