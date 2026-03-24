using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AppInitializer : MonoBehaviour
{
    [SerializeField] private FirebaseManager firebaseManager;
    [SerializeField] private UserDataService userDataService;
    [SerializeField] private AppVersionChecker appVersionChecker;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "LobbyScene") return;
        InitializeLobby();
    }

    private void Start()
    {
        InitializeLobby();
    }

    private void InitializeLobby()
    {
        if (LobbyManager.Instance == null) return;

        GooglePlayGamesManager.Instance.Initialize();

        // 이미 BootScene에서 Show했지만 혹시 모를 경우 대비
        LoadingUI.Instance?.Show();

        var cachedUserData = GameDataTransfer.Instance?.CurrentUserData;
        if (cachedUserData != null)
        {
            LoadingUI.Instance?.Hide();
            SoundManager.Instance?.PlayLobbyBGM();
            LobbyManager.Instance.Initialize(cachedUserData.currentStage);
            return;
        }

        firebaseManager.Initialize(() =>
        {
            userDataService.Initialize();
            appVersionChecker.CheckVersion(() =>
            {
                firebaseManager.SignInAnonymous(() =>
                {
                    string userId = firebaseManager.CurrentUser.UserId;
                    userDataService.LoadUserData(userId, userData =>
                    {
                        GameDataTransfer.Instance.SetUserData(userData);
                        LoadingUI.Instance?.Hide();
                        SoundManager.Instance?.PlayLobbyBGM();
                        LobbyManager.Instance.Initialize(userData.currentStage);
                    });
                },
                error =>
                {
                    LoadingUI.Instance?.ShowError("네트워크 연결을 확인해주세요.");
                });
            });
        });
    }
}