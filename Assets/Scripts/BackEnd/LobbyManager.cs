using System;
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
    [SerializeField] private GameObject retryPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button resumeCloseButton;


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
        UpdateGoogleLinkButton();
        UnityAdsManager.Instance?.HideBanner();

        UpdateResumeButton();

        // 캐싱된 데이터로 먼저 UI 업데이트
        var cachedUserData = GameDataTransfer.Instance?.CurrentUserData;
        if (cachedUserData != null)
        {
            currentStage = cachedUserData.currentStage;
            UpdateStageUI();
            LobbyUIManager.Instance?.UpdateUI(cachedUserData);
        }

        NetworkChecker.Instance?.Check(() =>
        {
            string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(userId)) return;

            UserDataService.Instance?.LoadUserData(userId, userData =>
            {
                currentStage = userData.currentStage;
                GameDataTransfer.Instance.SetUserData(userData);
                UpdateStageUI();
                LobbyUIManager.Instance?.UpdateUI(userData);
            });
        });
    }
    private void UpdateResumeButton()
    {
        bool hasSave = GameSaveService.Instance?.HasSaveData() ?? false;
        retryPanel?.SetActive(hasSave);

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(OnResumeButtonClicked);
        }

        if (resumeCloseButton != null)
        {
            resumeCloseButton.onClick.RemoveAllListeners();
            resumeCloseButton.onClick.AddListener(OnResumeCloseButtonClicked);
        }
    }
    private void OnResumeCloseButtonClicked()
    {
        GameSaveService.Instance?.ClearLocal();
        GameSaveService.Instance?.ClearFirestore();
        retryPanel?.SetActive(false);

        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;

        string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId)) return;

        bool wasFullBefore = userData.stamina >= StaminaChargeCalculator.MaxStamina;
        userData.stamina = Mathf.Max(0, userData.stamina - 1);

        if (wasFullBefore)
            userData.staminaLastChargeTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        GameDataTransfer.Instance.SetUserData(userData);
        UserDataService.Instance?.UpdateStaminaData(userId, userData.stamina, userData.staminaLastChargeTime);
        LobbyUIManager.Instance?.UpdateUI(userData);
    }

    private void OnResumeButtonClicked()
    {
        var saveData = GameSaveService.Instance?.LoadLocal();
        if (saveData == null) { resumeButton.gameObject.SetActive(false); return; }

        var levelData = levelDataBase.Get(saveData.stageIndex);
        if (levelData == null) { GameSaveService.Instance?.ClearLocal(); return; }

        GameDataTransfer.Instance.SetLevelData(levelData);
        GameDataTransfer.Instance.SetSaveData(saveData);
        SceneLoader.LoadGameAsync(LoadingUI.Instance);
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