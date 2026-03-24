using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;

public class SettingPanel : MonoBehaviour
{
    [SerializeField] private Button exitBtn;
    [SerializeField] private Button lobbyBtn;
    [SerializeField] private Button retryBtn;

    [Header("Toggles")]
    [SerializeField] private Button sfxToggleBtn;
    [SerializeField] private RectTransform sfxHandle;
    [SerializeField] private Button bgmToggleBtn;
    [SerializeField] private RectTransform bgmHandle;
    [SerializeField] private Button vibrationToggleBtn;
    [SerializeField] private RectTransform vibrationHandle;

    private const float OnX = 60f;
    private const float OffX = -60f;
    private const float ToggleDuration = 0.1f;

    private void OnEnable()
    {
        exitBtn?.onClick.AddListener(OnExitClicked);
        lobbyBtn?.onClick.AddListener(OnLobbyClicked);
        retryBtn?.onClick.AddListener(OnRetryClicked);
        sfxToggleBtn?.onClick.AddListener(OnSfxToggled);
        bgmToggleBtn?.onClick.AddListener(OnBgmToggled);

        InitHandle(sfxHandle, SoundManager.Instance.IsSoundEnabled());
        InitHandle(bgmHandle, SoundManager.Instance.IsMusicEnabled());

        GameManager.Instance?.FreezeTimer(float.MaxValue);
    }

    private void OnDisable()
    {
        exitBtn?.onClick.RemoveAllListeners();
        lobbyBtn?.onClick.RemoveAllListeners();
        retryBtn?.onClick.RemoveAllListeners();
        sfxToggleBtn?.onClick.RemoveAllListeners();
        bgmToggleBtn?.onClick.RemoveAllListeners();
        vibrationToggleBtn?.onClick.RemoveAllListeners();

        sfxHandle?.DOKill();
        bgmHandle?.DOKill();
        vibrationHandle?.DOKill();

        GameManager.Instance?.ResumeTimer();
    }

    private void InitHandle(RectTransform handle, bool isOn)
    {
        if (handle == null) return;
        handle.anchoredPosition = new Vector2(isOn ? OnX : OffX, handle.anchoredPosition.y);
    }

    private void AnimateHandle(RectTransform handle, bool isOn)
    {
        if (handle == null) return;
        handle.DOKill();
        handle.DOAnchorPosX(isOn ? OnX : OffX, ToggleDuration).SetEase(Ease.OutQuad);
    }

    private void OnSfxToggled()
    {
        bool next = !SoundManager.Instance.IsSoundEnabled();
        SoundManager.Instance.SetSoundEnabled(next);
        AnimateHandle(sfxHandle, next);
    }

    private void OnBgmToggled()
    {
        bool next = !SoundManager.Instance.IsMusicEnabled();
        SoundManager.Instance.SetMusicEnabled(next);
        AnimateHandle(bgmHandle, next);
    }

    private void OnExitClicked() => gameObject.SetActive(false);

    private void OnLobbyClicked()
    {
        GameSaveService.Instance?.ClearLocal();
        GameSaveService.Instance?.ClearFirestore();
        ConsumeStaminaAndExecute(() => SceneLoader.LoadLobby());
    }

    private void OnRetryClicked()
    {
        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData != null && userData.stamina < 1)
        {
            gameObject.SetActive(false);
            GameManager.Instance?.ShowAddStaminaPanel();
            return;
        }

        ConsumeStaminaAndExecute(() => SceneLoader.LoadGameAsync());
    }

    private void ConsumeStaminaAndExecute(Action onComplete)
    {
        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) { onComplete?.Invoke(); return; }

        string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId)) { onComplete?.Invoke(); return; }

        bool wasFullBefore = userData.stamina >= StaminaChargeCalculator.MaxStamina;
        userData.stamina = Mathf.Max(0, userData.stamina - 1);

        if (wasFullBefore)
            userData.staminaLastChargeTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        GameDataTransfer.Instance.SetUserData(userData);
        UserDataService.Instance?.UpdateStaminaData(userId, userData.stamina, userData.staminaLastChargeTime, onComplete);
    }
}