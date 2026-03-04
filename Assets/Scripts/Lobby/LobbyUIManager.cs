using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class LobbyUIManager : MonoBehaviour
{
    public static LobbyUIManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI staminaText;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private GameObject staminaCharging;
    [SerializeField] private TextMeshProUGUI staminaChargingText;

    private Coroutine chargeCoroutine;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (chargeCoroutine != null)
            StopCoroutine(chargeCoroutine);
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

            timeLeft = 20f * 60f;
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