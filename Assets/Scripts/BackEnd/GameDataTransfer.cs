using UnityEngine;

public class GameDataTransfer : MonoBehaviour
{
    public static GameDataTransfer Instance { get; private set; }

    public LevelData CurrentLevelData { get; private set; }
    public UserData CurrentUserData { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetLevelData(LevelData data) => CurrentLevelData = data;
    public void SetUserData(UserData data) => CurrentUserData = data;
}