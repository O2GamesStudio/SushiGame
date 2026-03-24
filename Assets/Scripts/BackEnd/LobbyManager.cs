using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using DG.Tweening;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [SerializeField] private CoinPassView coinPassView;
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
    [SerializeField] private Button storeButton;
    [SerializeField] private RectTransform storeRect;

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
        storeButton?.onClick.RemoveAllListeners();
        storeButton?.onClick.AddListener(OnStoreButtonClicked);
        LobbyUIManager.Instance?.RefreshDailyRewardAlert();
        UnityAdsManager.Instance?.DestroyBanner();
        UpdateResumeButton();

        var cachedUserData = GameDataTransfer.Instance?.CurrentUserData;
        if (cachedUserData != null)
        {
            currentStage = cachedUserData.currentStage;
            UpdateStageUI();
            LobbyUIManager.Instance?.UpdateUI(cachedUserData);

            string cachedUserId = FirebaseManager.Instance?.CurrentUser?.UserId;
            if (!string.IsNullOrEmpty(cachedUserId))
            {
                CoinPassManager.Instance?.Initialize(cachedUserData, cachedUserId);
                coinPassView?.Initialize();
            }
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
                CoinPassManager.Instance?.Initialize(userData, userId);
                coinPassView?.Initialize();
                LobbyUIManager.Instance?.RefreshPassAlert();
                LobbyUIManager.Instance?.RefreshDailyRewardAlert();
            });
        });
    }

    private void OnStoreButtonClicked()
    {
        storeRect.DOKill();
        storeRect.anchoredPosition = new Vector2(-1150f, 0f);
        storeRect.DOAnchorPosX(0f, 0.6f)
            .SetEase(Ease.OutBack, 1.1f);
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