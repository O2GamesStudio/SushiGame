using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AddStaminaPanel : MonoBehaviour
{
    [SerializeField] private Button adButton;
    [SerializeField] private Button goldButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private TextMeshProUGUI goldCostText;
    [SerializeField] private TextMeshProUGUI adLimitText;
    [SerializeField] private GameObject bgToClose;

    private const int GoldCost = 1000;
    private const int StaminaPerGold = 5;
    private const int StaminaPerAd = 1;
    private const int MaxDailyAdCount = 5;
    private const string AdCountKey = "StaminaAdCount";
    private const string AdDateKey = "StaminaAdDate";

    private void OnEnable()
    {
        adButton?.onClick.AddListener(OnAdButtonClicked);
        goldButton?.onClick.AddListener(OnGoldButtonClicked);
        exitButton?.onClick.AddListener(OnExitClicked);

        if (goldCostText != null)
            goldCostText.text = $"{GoldCost}";

        RefreshAdButton();
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

    private void RefreshAdButton()
    {
        bool isAdsRemoved = GameDataTransfer.Instance?.CurrentUserData?.isAdsRemoved ?? false;
        int remainingCount = GetRemainingAdCount();

        if (isAdsRemoved)
        {
            // 광고제거 구매 시 횟수 제한 없이 바로 충전
            adButton.interactable = true;
            if (adLimitText != null) adLimitText.text = $"{remainingCount}/{MaxDailyAdCount}";
        }
        else
        {
            adButton.interactable = remainingCount > 0;
            if (adLimitText != null) adLimitText.text = $"{remainingCount}/{MaxDailyAdCount}";
        }
    }

    private int GetRemainingAdCount()
    {
        string today = System.DateTime.UtcNow.ToString("yyyyMMdd");
        string savedDate = PlayerPrefs.GetString(AdDateKey, "");

        if (savedDate != today)
        {
            PlayerPrefs.SetString(AdDateKey, today);
            PlayerPrefs.SetInt(AdCountKey, 0);
            return MaxDailyAdCount;
        }

        int usedCount = PlayerPrefs.GetInt(AdCountKey, 0);
        return Mathf.Max(0, MaxDailyAdCount - usedCount);
    }

    private void IncrementAdCount()
    {
        int usedCount = PlayerPrefs.GetInt(AdCountKey, 0);
        PlayerPrefs.SetInt(AdCountKey, usedCount + 1);
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
        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;
        if (userData.stamina >= StaminaChargeCalculator.MaxStamina) return;

        bool isAdsRemoved = userData.isAdsRemoved;

        if (isAdsRemoved)
        {
            if (GetRemainingAdCount() <= 0) return;
            IncrementAdCount();
            AddStamina(StaminaPerAd);
            RefreshAdButton();
            return;
        }

        if (GetRemainingAdCount() <= 0) return;
        if (UnityAdsManager.Instance == null) return;

        UnityAdsManager.Instance.OnRewardEarned += OnAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow += OnAdFailed;
        UnityAdsManager.Instance.ShowRewardedAd();
    }

    private void OnAdRewardEarned()
    {
        UnityAdsManager.Instance.OnRewardEarned -= OnAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow -= OnAdFailed;
        IncrementAdCount();
        AddStamina(StaminaPerAd);
        RefreshAdButton();
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