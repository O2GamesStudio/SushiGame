using System;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine;

public class UserDataService : MonoBehaviour
{
    public static UserDataService Instance { get; private set; }

    private FirebaseFirestore db;
    private const string UsersCollection = "users";

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Initialize()
    {
        db = FirebaseFirestore.DefaultInstance;
    }

    public void LoadUserData(string userId, Action<UserData> onSuccess, Action<string> onFailed = null)
    {
        db.Collection(UsersCollection).Document(userId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onFailed?.Invoke(task.Exception?.Message ?? "Unknown error");
                return;
            }

            var snapshot = task.Result;
            if (snapshot.Exists)
            {
                var data = snapshot.ConvertTo<UserData>();

                bool needsUpdate = false;
                if (!snapshot.ContainsField("itemRandomRemover")) { data.itemRandomRemover = 1; needsUpdate = true; }
                if (!snapshot.ContainsField("itemTargetRemover")) { data.itemTargetRemover = 1; needsUpdate = true; }
                if (!snapshot.ContainsField("itemTimeFreezer")) { data.itemTimeFreezer = 1; needsUpdate = true; }
                if (!snapshot.ContainsField("itemShuffler")) { data.itemShuffler = 1; needsUpdate = true; }
                if (!snapshot.ContainsField("stamina")) { data.stamina = 5; needsUpdate = true; }
                if (!snapshot.ContainsField("coin")) { data.coin = 0; needsUpdate = true; }
                if (!snapshot.ContainsField("staminaLastChargeTime")) { data.staminaLastChargeTime = 0; needsUpdate = true; }
                if (!snapshot.ContainsField("hasCoinPass")) { data.hasCoinPass = false; needsUpdate = true; }
                if (!snapshot.ContainsField("passLevel")) { data.passLevel = 1; needsUpdate = true; }
                if (!snapshot.ContainsField("passXP")) { data.passXP = 0; needsUpdate = true; }
                if (!snapshot.ContainsField("claimedFreeRewards")) { data.claimedFreeRewards = new List<int>(); needsUpdate = true; }
                if (!snapshot.ContainsField("claimedPassRewards")) { data.claimedPassRewards = new List<int>(); needsUpdate = true; }
                if (!snapshot.ContainsField("isAdsRemoved")) { data.isAdsRemoved = false; needsUpdate = true; }
                if (!snapshot.ContainsField("dailyRewardDay")) { data.dailyRewardDay = 1; needsUpdate = true; }
                if (!snapshot.ContainsField("dailyRewardLastClaimTime")) { data.dailyRewardLastClaimTime = 0; needsUpdate = true; }

                if (needsUpdate)
                    SaveUserData(userId, data);

                onSuccess?.Invoke(data);
            }
            else
            {
                var newData = new UserData { currentStage = 1 };
                SaveUserData(userId, newData, () => onSuccess?.Invoke(newData));
            }
        });
    }

    public void SaveUserData(string userId, UserData data, Action onSuccess = null, Action<string> onFailed = null)
    {
        db.Collection(UsersCollection).Document(userId).SetAsync(data).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onFailed?.Invoke(task.Exception?.Message ?? "Unknown error");
                return;
            }
            onSuccess?.Invoke();
        });
    }

    public void UpdateStage(string userId, int stage, Action onSuccess = null, Action<string> onFailed = null)
    {
        var update = new Dictionary<string, object> { { "currentStage", stage } };

        db.Collection(UsersCollection).Document(userId).UpdateAsync(update).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onFailed?.Invoke(task.Exception?.Message ?? "Unknown error");
                return;
            }
            onSuccess?.Invoke();
        });
    }

    public void UpdateItemCount(string userId, string itemKey, int newCount, Action onSuccess = null, Action<string> onFailed = null)
    {
        var update = new Dictionary<string, object> { { itemKey, newCount } };

        db.Collection(UsersCollection).Document(userId).UpdateAsync(update).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onFailed?.Invoke(task.Exception?.Message ?? "Unknown error");
                return;
            }
            onSuccess?.Invoke();
        });
    }

    public void UpdateCurrency(string userId, int stamina, int coin, Action onSuccess = null, Action<string> onFailed = null)
    {
        var update = new Dictionary<string, object>
        {
            { "stamina", stamina },
            { "coin", coin }
        };

        db.Collection(UsersCollection).Document(userId).UpdateAsync(update).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onFailed?.Invoke(task.Exception?.Message ?? "Unknown error");
                return;
            }
            onSuccess?.Invoke();
        });
    }

    public void UpdateStaminaData(string userId, int stamina, long lastChargeTime, Action onSuccess = null, Action<string> onFailed = null)
    {
        var update = new Dictionary<string, object>
        {
            { "stamina", stamina },
            { "staminaLastChargeTime", lastChargeTime }
        };

        db.Collection(UsersCollection).Document(userId).UpdateAsync(update).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onFailed?.Invoke(task.Exception?.Message ?? "Unknown error");
                return;
            }
            onSuccess?.Invoke();
        });
    }
    public void UpdatePassData(string userId, int passLevel, int passXP)
    {
        var updates = new Dictionary<string, object>
    {
        { "passLevel", passLevel },
        { "passXP", passXP }
    };
        db.Collection("users").Document(userId).UpdateAsync(updates);
    }
    public void UpdateFields(string userId, Dictionary<string, object> updates, Action onSuccess = null)
    {
        db.Collection("users").Document(userId).UpdateAsync(updates).ContinueWithOnMainThread(task =>
        {
            if (!task.IsFaulted && !task.IsCanceled)
                onSuccess?.Invoke();
        });
    }

    public void UpdatePassPurchase(string userId, bool hasCoinPass)
    {
        var updates = new Dictionary<string, object>
    {
        { "hasCoinPass", hasCoinPass }
    };
        db.Collection("users").Document(userId).UpdateAsync(updates);
    }

    public void UpdatePassClaimedRewards(string userId, List<int> claimedFreeRewards, List<int> claimedPassRewards)
    {
        var updates = new Dictionary<string, object>
    {
        { "claimedFreeRewards", claimedFreeRewards },
        { "claimedPassRewards", claimedPassRewards }
    };
        db.Collection("users").Document(userId).UpdateAsync(updates);
    }
}

[FirestoreData]
public class UserData
{
    [FirestoreProperty] public int currentStage { get; set; } = 1;
    [FirestoreProperty] public long lastPlayedAt { get; set; } = 0;
    [FirestoreProperty] public int itemRandomRemover { get; set; } = 1;
    [FirestoreProperty] public int itemTargetRemover { get; set; } = 1;
    [FirestoreProperty] public int itemTimeFreezer { get; set; } = 1;
    [FirestoreProperty] public int itemShuffler { get; set; } = 1;
    [FirestoreProperty] public int stamina { get; set; } = 5;
    [FirestoreProperty] public int coin { get; set; } = 0;
    [FirestoreProperty] public long staminaLastChargeTime { get; set; } = 0;
    [FirestoreProperty] public bool hasCoinPass { get; set; } = false;
    [FirestoreProperty] public int passLevel { get; set; } = 1;
    [FirestoreProperty] public int passXP { get; set; } = 0;
    [FirestoreProperty] public List<int> claimedFreeRewards { get; set; } = new List<int>();
    [FirestoreProperty] public List<int> claimedPassRewards { get; set; } = new List<int>();
    [FirestoreProperty] public bool isAdsRemoved { get; set; } = false;
    [FirestoreProperty] public int dailyRewardDay { get; set; } = 1;
    [FirestoreProperty] public long dailyRewardLastClaimTime { get; set; } = 0;
}
