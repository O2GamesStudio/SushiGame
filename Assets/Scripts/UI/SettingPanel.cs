using UnityEngine;
using UnityEngine.UI;
using System;

public class SettingPanel : MonoBehaviour
{
    [SerializeField] private Button exitBtn;
    [SerializeField] private Button lobbyBtn;
    [SerializeField] private Button retryBtn;

    private void OnEnable()
    {
        exitBtn?.onClick.AddListener(OnExitClicked);
        lobbyBtn?.onClick.AddListener(OnLobbyClicked);
        retryBtn?.onClick.AddListener(OnRetryClicked);
        GameManager.Instance?.FreezeTimer(float.MaxValue);
    }

    private void OnDisable()
    {
        exitBtn?.onClick.RemoveAllListeners();
        lobbyBtn?.onClick.RemoveAllListeners();
        retryBtn?.onClick.RemoveAllListeners();
        GameManager.Instance?.ResumeTimer();
    }

    private void OnExitClicked()
    {
        gameObject.SetActive(false);
    }

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

        ConsumeStaminaAndExecute(() => SceneLoader.LoadGameAsync(LoadingUI.Instance));
    }

    private void ConsumeStaminaAndExecute(System.Action onComplete)
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