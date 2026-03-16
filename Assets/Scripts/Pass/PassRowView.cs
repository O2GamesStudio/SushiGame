using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PassRowView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI passNumText;
    [SerializeField] private Sprite receiveImage;
    [SerializeField] private Sprite lockSprite;

    [Header("Free Reward")]
    [SerializeField] private GameObject freeRewardObj;
    [SerializeField] private Image freeRewardImage;
    [SerializeField] private TextMeshProUGUI freeAmountText;
    [SerializeField] private GameObject freeLockImage;
    [SerializeField] private Image freeStateImage;
    [SerializeField] private Button freeRewardBtn;

    [Header("Pass Reward")]
    [SerializeField] private GameObject passRewardObj;
    [SerializeField] private Image passRewardImage;
    [SerializeField] private TextMeshProUGUI passAmountText;
    [SerializeField] private GameObject passLockImage;
    [SerializeField] private Image passStateImage;
    [SerializeField] private Button passRewardBtn;

    [Header("Reward Sprites")]
    [SerializeField] private Sprite coinSprite;
    [SerializeField] private Sprite itemRandomRemoverSprite;
    [SerializeField] private Sprite itemTargetRemoverSprite;
    [SerializeField] private Sprite itemTimeFreezerSprite;
    [SerializeField] private Sprite itemShufflerSprite;

    private int level;
    private bool isCooldown = false;

    public void Setup(int level, PassLevelData data)
    {
        this.level = level;
        if (passNumText != null) passNumText.text = level.ToString();

        if (data.freeReward != null && data.freeReward.amount > 0)
        {
            freeRewardObj?.SetActive(true);
            SetupReward(freeRewardImage, freeAmountText, data.freeReward);
        }
        else
        {
            freeRewardObj?.SetActive(false);
        }

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
        bool passClaimed = manager.IsPassRewardClaimed(level);

        if (freeRewardObj != null && freeRewardObj.activeSelf)
        {
            freeLockImage?.SetActive(!isUnlocked);
            if (freeRewardBtn != null)
                freeRewardBtn.interactable = isUnlocked && !freeClaimed;
            if (freeStateImage != null)
                freeStateImage.sprite = freeClaimed ? receiveImage : lockSprite;
        }

        passLockImage?.SetActive(!isUnlocked || !hasPass);
        if (passRewardBtn != null)
            passRewardBtn.interactable = isUnlocked && hasPass && !passClaimed && !isCooldown;
        if (passStateImage != null)
            passStateImage.sprite = passClaimed ? receiveImage : lockSprite;
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
        if (isCooldown) return;
        if (CoinPassManager.Instance.ClaimFreeReward(level))
        {
            Refresh();
            StartCoroutine(CooldownCoroutine());
            var userData = GameDataTransfer.Instance?.CurrentUserData;
            if (userData != null) LobbyUIManager.Instance?.UpdateCoinUI(userData.coin);
            LobbyUIManager.Instance?.RefreshPassAlert();
        }
    }

    private void OnPassRewardClicked()
    {
        if (isCooldown) return;
        if (CoinPassManager.Instance.ClaimPassReward(level))
        {
            Refresh();
            StartCoroutine(CooldownCoroutine());
            var userData = GameDataTransfer.Instance?.CurrentUserData;
            if (userData != null) LobbyUIManager.Instance?.UpdateCoinUI(userData.coin);
            LobbyUIManager.Instance?.RefreshPassAlert();
        }
    }

    private IEnumerator CooldownCoroutine()
    {
        isCooldown = true;
        if (freeRewardBtn != null) freeRewardBtn.interactable = false;
        if (passRewardBtn != null) passRewardBtn.interactable = false;

        yield return new WaitForSeconds(0.05f);

        isCooldown = false;
        Refresh();
    }

    private void OnDestroy()
    {
        freeRewardBtn?.onClick.RemoveAllListeners();
        passRewardBtn?.onClick.RemoveAllListeners();
    }
}