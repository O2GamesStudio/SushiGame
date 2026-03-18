using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CoinPassManager : MonoBehaviour
{
    public static CoinPassManager Instance { get; private set; }

    [SerializeField] private PassDataBase passDataBase;
    [SerializeField] private Button exitBtn;
    [SerializeField] private GameObject coinPassBG;

    private UserData userData;
    private string userId;

    private void Awake() => Instance = this;

    public void Initialize(UserData data, string uid)
    {
        userData = data;
        userId = uid;
        ApplyPendingXP();
    }

    private void ApplyPendingXP()
    {
        int mergedCount = GameDataTransfer.Instance.LastMergedCount;
        if (mergedCount <= 0) return;

        AddXP(mergedCount);
        GameDataTransfer.Instance.ClearLastMergedCount();
    }
    public PassLevelData GetLevelData(int level) => passDataBase.Get(level);
    public void AddXP(int amount)
    {
        if (userData == null) return;

        userData.passXP += amount;
        CheckLevelUp();

        UserDataService.Instance?.UpdatePassData(userId, userData.passLevel, userData.passXP);
        GameDataTransfer.Instance.SetUserData(userData);
    }

    private void CheckLevelUp()
    {
        if (userData.passLevel >= passDataBase.MaxLevel) return;

        var levelData = passDataBase.Get(userData.passLevel);
        if (levelData == null) return;

        while (userData.passXP >= levelData.requiredXP && userData.passLevel < passDataBase.MaxLevel)
        {
            userData.passXP -= levelData.requiredXP;
            userData.passLevel++;
            levelData = passDataBase.Get(userData.passLevel);
            if (levelData == null) break;
        }
    }

    public void BuyPass()
    {
        if (userData == null || userData.hasCoinPass) return;

        userData.hasCoinPass = true;
        GameDataTransfer.Instance.SetUserData(userData);
        UserDataService.Instance?.UpdatePassPurchase(userId, true);
    }

    public bool ClaimFreeReward(int level)
    {
        Debug.Log($"[CoinPassManager] ClaimFreeReward level:{level} claimedFreeRewards:{string.Join(",", userData.claimedFreeRewards)}");
        if (userData == null) return false;
        if (userData.passLevel < level) return false;
        if (userData.claimedFreeRewards.Contains(level)) return false;

        var levelData = passDataBase.Get(level);
        if (levelData == null) return false;

        GrantReward(levelData.freeReward);
        userData.claimedFreeRewards.Add(level);
        GameDataTransfer.Instance.SetUserData(userData);
        UserDataService.Instance?.UpdatePassClaimedRewards(userId, userData.claimedFreeRewards, userData.claimedPassRewards);
        return true;
    }

    public bool ClaimPassReward(int level)
    {
        if (userData == null) return false;
        if (!userData.hasCoinPass) return false;
        if (userData.passLevel < level) return false;
        if (userData.claimedPassRewards.Contains(level)) return false;

        var levelData = passDataBase.Get(level);
        if (levelData == null) return false;

        GrantReward(levelData.passReward);
        userData.claimedPassRewards.Add(level);
        GameDataTransfer.Instance.SetUserData(userData);
        UserDataService.Instance?.UpdatePassClaimedRewards(userId, userData.claimedFreeRewards, userData.claimedPassRewards);
        return true;
    }

    private void GrantReward(PassRewardData reward)
    {
        if (reward == null) return;

        switch (reward.rewardType)
        {
            case RewardType.Coin:
                userData.coin += reward.amount;
                break;
            case RewardType.ItemRandomRemover:
                userData.itemRandomRemover += reward.amount;
                break;
            case RewardType.ItemTargetRemover:
                userData.itemTargetRemover += reward.amount;
                break;
            case RewardType.ItemTimeFreezer:
                userData.itemTimeFreezer += reward.amount;
                break;
            case RewardType.ItemShuffler:
                userData.itemShuffler += reward.amount;
                break;
        }

        if (string.IsNullOrEmpty(userId)) return;

        var updates = new Dictionary<string, object>
    {
        { "coin", userData.coin },
        { "itemRandomRemover", userData.itemRandomRemover },
        { "itemTargetRemover", userData.itemTargetRemover },
        { "itemTimeFreezer", userData.itemTimeFreezer },
        { "itemShuffler", userData.itemShuffler }
    };

        UserDataService.Instance?.UpdateFields(userId, updates);
    }

    public float GetLevelProgress()
    {
        if (userData == null) return 0f;
        var levelData = passDataBase.Get(userData.passLevel);
        if (levelData == null) return 1f;
        return (float)userData.passXP / levelData.requiredXP;
    }
    private void OnEnable()
    {
        exitBtn?.onClick.AddListener(OnExitClicked);
    }

    private void OnDisable()
    {
        exitBtn?.onClick.RemoveAllListeners();
    }

    private void OnExitClicked()
    {
        coinPassBG?.SetActive(false);
        LobbyUIManager.Instance?.RefreshPassAlert();
    }

    public bool IsFreeRewardClaimed(int level) => userData?.claimedFreeRewards.Contains(level) ?? false;
    public bool IsPassRewardClaimed(int level) => userData?.claimedPassRewards.Contains(level) ?? false;
    public bool IsLevelUnlocked(int level) => userData?.passLevel >= level;
    public bool HasPass() => userData?.hasCoinPass ?? false;
}