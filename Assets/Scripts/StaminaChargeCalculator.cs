using System;
using UnityEngine;

public static class StaminaChargeCalculator
{
    public const int MaxStamina = 5;
    public const long ChargeIntervalMs = 20 * 60 * 1000L;
    public const float ChargeIntervalSeconds = ChargeIntervalMs / 1000f;

    public static (int newStamina, long newLastChargeTime, float remainingSeconds) Calculate(
        int currentStamina, long lastChargeTime)
    {
        if (currentStamina >= MaxStamina)
            return (currentStamina, lastChargeTime, 0f);

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        if (lastChargeTime <= 0)
            lastChargeTime = now;

        long elapsed = now - lastChargeTime;
        int chargesEarned = (int)(elapsed / ChargeIntervalMs);
        long remainder = elapsed % ChargeIntervalMs;

        int newStamina = Mathf.Min(MaxStamina, currentStamina + chargesEarned);
        long newLastChargeTime = now - remainder;
        float remainingSeconds = (ChargeIntervalMs - remainder) / 1000f;

        return (newStamina, newLastChargeTime, remainingSeconds);
    }
}