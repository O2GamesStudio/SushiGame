using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    public static LobbyUIManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI staminaText;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private GameObject staminaCharging;
    [SerializeField] private TextMeshProUGUI staminaChargingText;

    [Header("LeftButtons")]
    [SerializeField] private Button removeAdsBtn;
    [SerializeField] private RemoveAdsPanel removePanel;

    [Header("RightButtons")]
    [SerializeField] private Button passGoldBtn;
    [SerializeField] private Button passItemBtn;

    [Header("Pass")]
    [SerializeField] private GameObject coinPassBG;
    [SerializeField] private GameObject goldPassAlert;

    [Header("Daily Reward")]
    [SerializeField] private GameObject dailyRewardAlert;
    [SerializeField] private Button dailyRewardBtn;
    [SerializeField] private GameObject dailyRewardBG;

    private Coroutine chargeCoroutine;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        removeAdsBtn?.onClick.AddListener(() => removePanel?.gameObject.SetActive(true));
        passGoldBtn?.onClick.AddListener(OnPassGoldBtnClicked);
        dailyRewardBtn?.onClick.AddListener(OnDailyRewardBtnClicked);
    }

    private void OnDestroy()
    {
        if (chargeCoroutine != null)
            StopCoroutine(chargeCoroutine);
        removeAdsBtn?.onClick.RemoveAllListeners();
        passGoldBtn?.onClick.RemoveAllListeners();
        dailyRewardBtn?.onClick.RemoveAllListeners();
    }
    private void OnDailyRewardBtnClicked()
    {
        dailyRewardBG?.SetActive(true);
        dailyRewardAlert?.SetActive(false);
    }

    public void RefreshDailyRewardAlert()
    {
        if (dailyRewardAlert == null) return;

        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) { dailyRewardAlert.SetActive(false); return; }

        bool isClaimedToday = false;
        if (userData.dailyRewardLastClaimTime > 0)
        {
            string lastClaimDate = DateTimeOffset.FromUnixTimeMilliseconds(userData.dailyRewardLastClaimTime)
                .UtcDateTime.ToString("yyyyMMdd");
            string today = DateTime.UtcNow.ToString("yyyyMMdd");
            isClaimedToday = lastClaimDate == today;
        }

        dailyRewardAlert.SetActive(!isClaimedToday);
    }

    private void OnPassGoldBtnClicked()
    {
        coinPassBG?.SetActive(true);
    }
    public void UpdateCoinUI(int coin)
    {
        if (coinText != null) coinText.text = coin.ToString();
    }
    public void RefreshPassAlert()
    {
        if (goldPassAlert == null) return;

        var manager = CoinPassManager.Instance;
        if (manager == null) { goldPassAlert.SetActive(false); return; }

        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) { goldPassAlert.SetActive(false); return; }

        bool hasUnclaimed = false;
        for (int i = 1; i <= userData.passLevel; i++)
        {
            var levelData = CoinPassManager.Instance?.GetLevelData(i);
            if (levelData == null) continue;

            if (levelData.freeReward != null && levelData.freeReward.amount > 0 && !manager.IsFreeRewardClaimed(i))
            {
                hasUnclaimed = true;
                break;
            }
            if (manager.HasPass() && !manager.IsPassRewardClaimed(i))
            {
                hasUnclaimed = true;
                break;
            }
        }

        goldPassAlert.SetActive(hasUnclaimed);
    }

    public void UpdateUI(UserData userData)
    {
        if (userData == null) return;
        if (staminaText != null) staminaText.text = userData.stamina.ToString();
        if (coinText != null) coinText.text = userData.coin.ToString();
        StartStaminaCharge(userData);
    }

    private void StartStaminaCharge(UserData userData)
    {
        if (chargeCoroutine != null)
            StopCoroutine(chargeCoroutine);

        if (userData.stamina >= StaminaChargeCalculator.MaxStamina)
        {
            staminaCharging?.SetActive(false);
            return;
        }

        var (newStamina, newLastChargeTime, remainingSeconds) =
            StaminaChargeCalculator.Calculate(userData.stamina, userData.staminaLastChargeTime);

        if (newStamina != userData.stamina || newLastChargeTime != userData.staminaLastChargeTime)
        {
            userData.stamina = newStamina;
            userData.staminaLastChargeTime = newLastChargeTime;
            GameDataTransfer.Instance?.SetUserData(userData);

            if (staminaText != null) staminaText.text = userData.stamina.ToString();
            SyncToFirebase(userData);
        }

        if (userData.stamina >= StaminaChargeCalculator.MaxStamina)
        {
            staminaCharging?.SetActive(false);
            return;
        }

        staminaCharging?.SetActive(true);
        chargeCoroutine = StartCoroutine(ChargeCoroutine(userData, remainingSeconds));
    }

    private IEnumerator ChargeCoroutine(UserData userData, float remainingSeconds)
    {
        float timeLeft = remainingSeconds;

        while (userData.stamina < StaminaChargeCalculator.MaxStamina)
        {
            while (timeLeft > 0)
            {
                timeLeft -= Time.deltaTime;
                if (staminaChargingText != null)
                    staminaChargingText.text = FormatTime(timeLeft);
                yield return null;
            }

            userData.stamina = Mathf.Min(StaminaChargeCalculator.MaxStamina, userData.stamina + 1);
            userData.staminaLastChargeTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            GameDataTransfer.Instance?.SetUserData(userData);

            if (staminaText != null) staminaText.text = userData.stamina.ToString();
            SyncToFirebase(userData);

            if (userData.stamina >= StaminaChargeCalculator.MaxStamina)
            {
                staminaCharging?.SetActive(false);
                yield break;
            }

            timeLeft = StaminaChargeCalculator.ChargeIntervalSeconds;
        }
    }

    private void SyncToFirebase(UserData userData)
    {
        string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId)) return;
        UserDataService.Instance?.UpdateStaminaData(userId, userData.stamina, userData.staminaLastChargeTime);
    }

    private string FormatTime(float seconds)
    {
        int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
        return $"{s / 60}m {s % 60}s";
    }
}