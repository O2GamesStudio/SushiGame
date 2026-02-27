using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [SerializeField] private LevelData[] levelDataList;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button googleLinkButton;
    [SerializeField] private LoadingUI loadingUI;

    private int currentStage = 1;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Initialize(int stage)
    {
        Debug.Log($"[LobbyManager] Initialize 호출: stage={stage}");
        currentStage = stage;
        UpdateStageUI();

        string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (!string.IsNullOrEmpty(userId))
        {
            UserDataService.Instance?.LoadUserData(userId, (userData) =>
            {
                Debug.Log($"[LobbyManager] Firestore 로드 완료: stage={userData.currentStage}");
                currentStage = userData.currentStage;
                GameDataTransfer.Instance.SetUserData(userData);
                UpdateStageUI();
            });
        }

        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(OnStartButtonClicked);

        googleLinkButton.onClick.RemoveAllListeners();
        googleLinkButton.onClick.AddListener(OnGoogleLinkButtonClicked);

        UpdateGoogleLinkButton();
    }

    private void UpdateStageUI()
    {
        Debug.Log($"[LobbyManager] UpdateStageUI 호출: currentStage={currentStage}");
        if (stageText != null)
            stageText.text = $"{currentStage}층";
        else
            Debug.LogError("[LobbyManager] stageText가 null입니다!");
    }

    private void UpdateGoogleLinkButton()
    {
        if (googleLinkButton != null)
            googleLinkButton.gameObject.SetActive(FirebaseManager.Instance.IsAnonymous);
    }
    private void OnStartButtonClicked()
    {
        int index = currentStage - 1;
        if (index < 0 || index >= levelDataList.Length) return;

        GameDataTransfer.Instance.SetLevelData(levelDataList[index]);
        SceneLoader.LoadGameAsync(LoadingUI.Instance);
    }

    private void OnGoogleLinkButtonClicked()
    {
        GooglePlayGamesManager.Instance?.StartGoogleSignIn(idToken =>
        {
            FirebaseManager.Instance.LinkWithGoogle(idToken,
                () => UpdateGoogleLinkButton(),
                null
            );
        });
    }
}