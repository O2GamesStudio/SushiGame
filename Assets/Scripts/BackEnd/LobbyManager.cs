using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [SerializeField] private LevelDataBase levelDataBase;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject addStaminaBG;
    [SerializeField] private Button staminaButton;
    [SerializeField] private GameObject retryPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button resumeCloseButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private LobbySettingPanel settingPanel;

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
        settingButton?.onClick.RemoveAllListeners();
        settingButton?.onClick.AddListener(() => settingPanel?.gameObject.SetActive(true));
        staminaButton?.onClick.RemoveAllListeners();
        staminaButton?.onClick.AddListener(() => addStaminaBG?.SetActive(true));

        UnityAdsManager.Instance?.HideBanner();
        UpdateResumeButton();

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

    private void OnStartButtonClicked()
    {
        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData != null && userData.stamina < 1)
        {
            addStaminaBG?.SetActive(true);
            return;
        }

        var levelData = levelDataBase.Get(currentStage);
        if (levelData == null) return;

        GameDataTransfer.Instance.SetLevelData(levelData);
        SceneLoader.LoadGameAsync(LoadingUI.Instance);
    }
}