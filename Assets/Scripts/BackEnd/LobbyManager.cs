using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [SerializeField] private LevelDataBase levelDataBase;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button googleLinkButton;
    [SerializeField] private GameObject addStaminaPanel;
    [SerializeField] private Button staminaButton;

    private int currentStage = 1;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Initialize(int stage)
    {
        currentStage = stage;
        UpdateStageUI();

        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(OnStartButtonClicked);

        googleLinkButton.onClick.RemoveAllListeners();
        googleLinkButton.onClick.AddListener(OnGoogleLinkButtonClicked);

        staminaButton?.onClick.RemoveAllListeners();
        staminaButton?.onClick.AddListener(() => addStaminaPanel?.SetActive(true));

        UpdateGoogleLinkButton();
        UnityAdsManager.Instance?.HideBanner();

        string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId)) return;

        UserDataService.Instance?.LoadUserData(userId, userData =>
        {
            currentStage = userData.currentStage;
            GameDataTransfer.Instance.SetUserData(userData);
            UpdateStageUI();
            LobbyUIManager.Instance?.UpdateUI(userData);
        });
    }

    private void UpdateStageUI()
    {
        if (stageText != null)
            stageText.text = $"Lv.{currentStage}";
    }

    private void UpdateGoogleLinkButton()
    {
        if (googleLinkButton != null)
            googleLinkButton.gameObject.SetActive(FirebaseManager.Instance.IsAnonymous);
    }

    private void OnStartButtonClicked()
    {
        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData != null && userData.stamina < 1)
        {
            addStaminaPanel?.SetActive(true);
            return;
        }

        var levelData = levelDataBase.Get(currentStage);
        if (levelData == null) return;

        GameDataTransfer.Instance.SetLevelData(levelData);
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