using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class DailyRewardPanel : MonoBehaviour
{
    [SerializeField] private DailyRewardDataBase rewardDataBase;
    [SerializeField] private DailyRewardItem[] rewardItems;
    [SerializeField] private Button exitBtn;

    private bool isClaiming = false;

    private void OnEnable()
    {
        exitBtn?.onClick.AddListener(OnExitClicked);
        Initialize();
    }

    private void OnDisable()
    {
        exitBtn?.onClick.RemoveAllListeners();
        isClaiming = false;
    }

    private void Initialize()
    {
        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;

        UpdateDayProgress(userData);

        bool claimedToday = IsAlreadyClaimedToday(userData);
        int currentDay = userData.dailyRewardDay;

        for (int i = 0; i < rewardItems.Length; i++)
        {
            int day = i + 1;
            var data = rewardDataBase.Get(day);
            if (data == null) continue;

            bool isClaimed = day < currentDay || (day == currentDay && claimedToday);
            rewardItems[i].Setup(day, data, OnClaimReward);
            rewardItems[i].Refresh(currentDay, isClaimed);
        }
    }

    private void UpdateDayProgress(UserData userData)
    {
        if (userData.dailyRewardLastClaimTime <= 0) return;

        string lastClaimDate = DateTimeOffset.FromUnixTimeMilliseconds(userData.dailyRewardLastClaimTime)
            .UtcDateTime.ToString("yyyyMMdd");
        string today = DateTime.UtcNow.ToString("yyyyMMdd");

        if (lastClaimDate == today) return;

        // 하루 건너뛰어도 다음 날 보상으로 진행
        if (userData.dailyRewardDay >= rewardDataBase.TotalDays)
            userData.dailyRewardDay = 0;
    }

    private bool IsAlreadyClaimedToday(UserData userData)
    {
        if (userData.dailyRewardLastClaimTime <= 0) return false;

        string lastClaimDate = DateTimeOffset.FromUnixTimeMilliseconds(userData.dailyRewardLastClaimTime)
            .UtcDateTime.ToString("yyyyMMdd");
        string today = DateTime.UtcNow.ToString("yyyyMMdd");

        return lastClaimDate == today;
    }

    private void OnClaimReward(int day)
    {
        if (isClaiming) return;

        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;
        if (IsAlreadyClaimedToday(userData)) return;

        string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId)) return;

        isClaiming = true;

        var data = rewardDataBase.Get(day);
        if (data == null) { isClaiming = false; return; }

        GrantReward(userData, data);

        userData.dailyRewardDay = day;
        userData.dailyRewardLastClaimTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        GameDataTransfer.Instance.SetUserData(userData);

        var updates = new Dictionary<string, object>
        {
            { "dailyRewardDay", userData.dailyRewardDay },
            { "dailyRewardLastClaimTime", userData.dailyRewardLastClaimTime },
            { "coin", userData.coin },
            { "itemRandomRemover", userData.itemRandomRemover },
            { "itemTargetRemover", userData.itemTargetRemover },
            { "itemTimeFreezer", userData.itemTimeFreezer },
            { "itemShuffler", userData.itemShuffler }
        };

        UserDataService.Instance?.UpdateFields(userId, updates, () =>
        {
            isClaiming = false;
            RefreshAll(userData);
            LobbyUIManager.Instance?.UpdateUI(userData);
            LobbyUIManager.Instance?.RefreshDailyRewardAlert();
        });
    }

    private void GrantReward(UserData userData, DailyRewardData data)
    {
        switch (data.rewardType)
        {
            case RewardType.Coin: userData.coin += data.amount; break;
            case RewardType.ItemRandomRemover: userData.itemRandomRemover += data.amount; break;
            case RewardType.ItemTargetRemover: userData.itemTargetRemover += data.amount; break;
            case RewardType.ItemTimeFreezer: userData.itemTimeFreezer += data.amount; break;
            case RewardType.ItemShuffler: userData.itemShuffler += data.amount; break;
        }
    }

    private void RefreshAll(UserData userData)
    {
        bool claimedToday = IsAlreadyClaimedToday(userData);
        int currentDay = userData.dailyRewardDay;

        for (int i = 0; i < rewardItems.Length; i++)
        {
            int day = i + 1;
            bool isClaimed = day < currentDay || (day == currentDay && claimedToday);
            rewardItems[i].Refresh(currentDay, isClaimed);
        }
    }

    private void OnExitClicked() => gameObject.SetActive(false);
}