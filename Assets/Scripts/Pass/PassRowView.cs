using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PassRowView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI passNumText;

    [Header("Free Reward")]
    [SerializeField] private GameObject freeRewardObj;
    [SerializeField] private Image freeRewardImage;
    [SerializeField] private TextMeshProUGUI freeAmountText;
    [SerializeField] private GameObject freeLockImage;
    [SerializeField] private Button freeRewardBtn;

    [Header("Pass Reward")]
    [SerializeField] private GameObject passRewardObj;
    [SerializeField] private Image passRewardImage;
    [SerializeField] private TextMeshProUGUI passAmountText;
    [SerializeField] private GameObject passLockImage;
    [SerializeField] private Button passRewardBtn;

    [Header("Reward Sprites")]
    [SerializeField] private Sprite coinSprite;
    [SerializeField] private Sprite itemRandomRemoverSprite;
    [SerializeField] private Sprite itemTargetRemoverSprite;
    [SerializeField] private Sprite itemTimeFreezerSprite;
    [SerializeField] private Sprite itemShufflerSprite;

    private int level;

    public void Setup(int level, PassLevelData data)
    {
        this.level = level;
        if (passNumText != null) passNumText.text = level.ToString();

        SetupReward(freeRewardImage, freeAmountText, data.freeReward);
        SetupReward(passRewardImage, passAmountText, data.passReward);

        freeRewardBtn?.onClick.AddListener(OnFreeRewardClicked);
        passRewardBtn?.onClick.AddListener(OnPassRewardClicked);

        Refresh();
    }

    public void Refresh()
    {
        var manager = CoinPassManager.Instance;
        if (manager == null) return;

        bool isUnlocked = manager.IsLevelUnlocked(level);
        bool hasPass = manager.HasPass();

        bool freeClaimed = manager.IsFreeRewardClaimed(level);
        freeLockImage?.SetActive(!isUnlocked || freeClaimed);
        freeRewardBtn.interactable = isUnlocked && !freeClaimed;

        bool passClaimed = manager.IsPassRewardClaimed(level);
        passLockImage?.SetActive(!isUnlocked || !hasPass || passClaimed);
        passRewardBtn.interactable = isUnlocked && hasPass && !passClaimed;
    }

    private void SetupReward(Image rewardImage, TextMeshProUGUI amountText, PassRewardData reward)
    {
        if (reward == null) return;
        if (amountText != null) amountText.text = reward.amount.ToString();
        if (rewardImage != null) rewardImage.sprite = GetRewardSprite(reward.rewardType);
    }

    private Sprite GetRewardSprite(RewardType rewardType)
    {
        return rewardType switch
        {
            RewardType.Coin => coinSprite,
            RewardType.ItemRandomRemover => itemRandomRemoverSprite,
            RewardType.ItemTargetRemover => itemTargetRemoverSprite,
            RewardType.ItemTimeFreezer => itemTimeFreezerSprite,
            RewardType.ItemShuffler => itemShufflerSprite,
            _ => null
        };
    }

    private void OnFreeRewardClicked()
    {
        if (CoinPassManager.Instance.ClaimFreeReward(level))
            Refresh();
    }

    private void OnPassRewardClicked()
    {
        if (CoinPassManager.Instance.ClaimPassReward(level))
            Refresh();
    }

    private void OnDestroy()
    {
        freeRewardBtn?.onClick.RemoveAllListeners();
        passRewardBtn?.onClick.RemoveAllListeners();
    }
}