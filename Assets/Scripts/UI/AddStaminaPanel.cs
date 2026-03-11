using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AddStaminaPanel : MonoBehaviour
{
    [SerializeField] private Button adButton;
    [SerializeField] private Button goldButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private TextMeshProUGUI goldCostText;
    [SerializeField] private GameObject bgToClose;

    private const int GoldCost = 1000;
    private const int StaminaPerGold = 5;
    private const int StaminaPerAd = 1;

    private void OnEnable()
    {
        adButton?.onClick.AddListener(OnAdButtonClicked);
        goldButton?.onClick.AddListener(OnGoldButtonClicked);
        exitButton?.onClick.AddListener(OnExitClicked);

        if (goldCostText != null)
            goldCostText.text = $"{GoldCost}";
    }

    private void OnDisable()
    {
        adButton?.onClick.RemoveAllListeners();
        goldButton?.onClick.RemoveAllListeners();
        exitButton?.onClick.RemoveAllListeners();

        if (UnityAdsManager.Instance != null)
        {
            UnityAdsManager.Instance.OnRewardEarned -= OnAdRewardEarned;
            UnityAdsManager.Instance.OnAdFailedToShow -= OnAdFailed;
        }
    }

    private void OnExitClicked()
    {
        if (bgToClose != null)
            bgToClose.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void OnAdButtonClicked()
    {
        if (UnityAdsManager.Instance == null) return;

        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;

        if (userData.stamina >= StaminaChargeCalculator.MaxStamina) return;

        UnityAdsManager.Instance.OnRewardEarned += OnAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow += OnAdFailed;
        UnityAdsManager.Instance.ShowRewardedAd();
    }

    private void OnAdRewardEarned()
    {
        UnityAdsManager.Instance.OnRewardEarned -= OnAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow -= OnAdFailed;
        AddStamina(StaminaPerAd);
    }

    private void OnAdFailed()
    {
        UnityAdsManager.Instance.OnRewardEarned -= OnAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow -= OnAdFailed;
    }

    private void OnGoldButtonClicked()
    {
        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;

        if (userData.coin < GoldCost) return;
        if (userData.stamina >= StaminaChargeCalculator.MaxStamina) return;

        string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId)) return;

        userData.coin -= GoldCost;
        GameDataTransfer.Instance.SetUserData(userData);

        UserDataService.Instance?.UpdateCurrency(userId, userData.stamina, userData.coin);
        AddStamina(StaminaPerGold);
    }

    private void AddStamina(int amount)
    {
        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;

        string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId)) return;

        bool wasFull = userData.stamina >= StaminaChargeCalculator.MaxStamina;
        userData.stamina += amount;

        if (wasFull)
            userData.staminaLastChargeTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        GameDataTransfer.Instance.SetUserData(userData);
        UserDataService.Instance?.UpdateStaminaData(userId, userData.stamina, userData.staminaLastChargeTime);

        LobbyUIManager.Instance?.UpdateUI(userData);
        GameManager.Instance?.RefreshStaminaUI(userData);
    }
}