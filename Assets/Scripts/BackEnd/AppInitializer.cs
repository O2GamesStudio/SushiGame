using UnityEngine;
using UnityEngine.SceneManagement;

public class AppInitializer : MonoBehaviour
{
    [SerializeField] private FirebaseManager firebaseManager;
    [SerializeField] private UserDataService userDataService;
    [SerializeField] private LoadingUI loadingUI;
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

        loadingUI?.Show();

        var cachedUserData = GameDataTransfer.Instance?.CurrentUserData;
        if (cachedUserData != null)
        {
            Debug.Log($"[AppInitializer] 캐시 데이터 사용: stage={cachedUserData.currentStage}");
            loadingUI?.Hide();
            LobbyManager.Instance.Initialize(cachedUserData.currentStage);
            return;
        }

        GooglePlayGamesManager.Instance.Initialize();

        firebaseManager.Initialize(() =>
        {
            userDataService.Initialize();
            appVersionChecker.CheckVersion(() =>
            {
                firebaseManager.SignInAnonymous(() =>
                {
                    string userId = firebaseManager.CurrentUser.UserId;
                    userDataService.LoadUserData(userId, (userData) =>
                    {
                        GameDataTransfer.Instance.SetUserData(userData);
                        loadingUI?.Hide();
                        LobbyManager.Instance.Initialize(userData.currentStage);
                    });
                },
                (error) =>
                {
                    loadingUI?.ShowError("네트워크 연결을 확인해주세요.");
                });
            });
        });
    }
}