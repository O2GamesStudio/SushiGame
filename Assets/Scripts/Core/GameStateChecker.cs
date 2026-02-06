using UnityEngine;

public class GameStateChecker : MonoBehaviour
{
    public static GameStateChecker Instance { get; private set; }

    private PlateManager plateManager;

    private void Awake()
    {
        Instance = this;
    }

    public void Initialize(PlateManager manager)
    {
        plateManager = manager;
    }

    public void CheckWinCondition()
    {
        if (plateManager.AreAllPlatesEmpty())
        {
            GameManager.Instance.OnGameWin();
        }
    }
}
