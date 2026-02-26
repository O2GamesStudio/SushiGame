using UnityEngine;

public class AppInitializer : MonoBehaviour
{
    [SerializeField] private FirebaseManager firebaseManager;
    [SerializeField] private UserDataService userDataService;
    [SerializeField] private LoadingUI loadingUI;

    private void Start()
    {
        loadingUI?.Show();

        GooglePlayGamesManager.Instance.Initialize();

        firebaseManager.Initialize(() =>
        {
            userDataService.Initialize();
            firebaseManager.SignInAnonymous(() =>
            {
                var cachedUserData = GameDataTransfer.Instance.CurrentUserData;
                if (cachedUserData != null)
                {
                    loadingUI?.Hide();
                    LobbyManager.Instance.Initialize(cachedUserData.currentStage);
                    return;
                }

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
    }
}