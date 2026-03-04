using UnityEngine;
using UnityEngine.UI;

public class StaminaButton : MonoBehaviour
{
    [SerializeField] private GameObject addStaminaPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button buyWithCoinButton;
    [SerializeField] private Button watchAdButton;
    [SerializeField] private int staminaCoinCost = 1000;

    private void Start()
    {
        closeButton?.onClick.AddListener(() => addStaminaPanel?.SetActive(false));
        buyWithCoinButton?.onClick.AddListener(OnBuyWithCoin);
        watchAdButton?.onClick.AddListener(OnWatchAd);
    }

    private void OnBuyWithCoin()
    {
        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;
        if (userData.coin < staminaCoinCost) return;

        string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId)) return;

        userData.coin -= staminaCoinCost;
        userData.stamina += 5;
        GameDataTransfer.Instance.SetUserData(userData);

        UserDataService.Instance?.UpdateStaminaData(userId, userData.stamina, userData.staminaLastChargeTime, () =>
        {
            UserDataService.Instance?.UpdateCurrency(userId, userData.stamina, userData.coin);
            RefreshUI(userData);
            addStaminaPanel?.SetActive(false);
        });
    }

    private void OnWatchAd()
    {
        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;
        if (userData.stamina >= StaminaChargeCalculator.MaxStamina) return;

        if (UnityAdsManager.Instance == null) return;

        UnityAdsManager.Instance.OnRewardEarned += OnAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow += OnAdFailed;
        UnityAdsManager.Instance.ShowRewardedAd();
    }

    private void OnAdRewardEarned()
    {
        UnityAdsManager.Instance.OnRewardEarned -= OnAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow -= OnAdFailed;

        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;

        string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId)) return;

        userData.stamina = Mathf.Min(StaminaChargeCalculator.MaxStamina, userData.stamina + 1);
        GameDataTransfer.Instance.SetUserData(userData);

        RefreshUI(userData);
        addStaminaPanel?.SetActive(false);

        UserDataService.Instance?.UpdateStaminaData(userId, userData.stamina, userData.staminaLastChargeTime);
    }

    private void OnAdFailed()
    {
        UnityAdsManager.Instance.OnRewardEarned -= OnAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow -= OnAdFailed;
    }

    private void RefreshUI(UserData userData)
    {
        if (LobbyUIManager.Instance != null)
            LobbyUIManager.Instance.UpdateUI(userData);

        if (GameManager.Instance != null)
            GameManager.Instance.RefreshStaminaUI(userData);
    }

    private void OnDestroy()
    {
        if (UnityAdsManager.Instance == null) return;
        UnityAdsManager.Instance.OnRewardEarned -= OnAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow -= OnAdFailed;
    }
}