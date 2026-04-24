using UnityEngine;
using UnityEngine.SceneManagement;

public class AppInitializer : MonoBehaviour
{
    [SerializeField] private UserDataService userDataService;

    private bool isLobbyInitialized = false;

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
        if (isLobbyInitialized) return;
        isLobbyInitialized = true;

        GooglePlayGamesManager.Instance.Initialize();

        var cachedUserData = GameDataTransfer.Instance?.CurrentUserData;
        if (cachedUserData != null)
        {
            LoadingUI.Instance?.Hide();
            SoundManager.Instance?.PlayLobbyBGM();
            LobbyManager.Instance.Initialize(cachedUserData.currentStage);
            return;
        }

        void Proceed()
        {
            userDataService.Initialize();
            FirebaseManager.Instance.SignInAnonymous(() =>
            {
                string userId = FirebaseManager.Instance.CurrentUser.UserId;
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
        }

        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsInitialized)
        {
            var go = new GameObject("FirebaseManager");
            var fm = go.AddComponent<FirebaseManager>();
            fm.Initialize(Proceed, error => LoadingUI.Instance?.ShowError("네트워크 연결을 확인해주세요."));
        }
        else
        {
            Proceed();
        }
    }
}