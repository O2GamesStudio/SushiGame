using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DailyRewardItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private Image rewardItemImage;
    [SerializeField] private TextMeshProUGUI rewardCountText;
    [SerializeField] private GameObject lockImage;
    [SerializeField] private Button rewardBtn;

    private int day;
    private System.Action<int> onClaim;

    public void Setup(int day, DailyRewardData data, System.Action<int> onClaim)
    {
        this.day = day;
        this.onClaim = onClaim;

        if (dayText != null) dayText.text = $"Day{day}";
        if (rewardItemImage != null) rewardItemImage.sprite = data.rewardSprite;
        if (rewardCountText != null) rewardCountText.text = $"x{data.amount}";

        rewardBtn?.onClick.AddListener(OnRewardBtnClicked);
    }

    public void Refresh(int currentDay, bool isClaimed)
    {
        bool isUnlocked = day <= currentDay;
        bool canClaim = day == currentDay && !isClaimed;

        lockImage?.SetActive(!isUnlocked || isClaimed);
        if (rewardBtn != null) rewardBtn.interactable = canClaim;
    }

    private void OnRewardBtnClicked()
    {
        onClaim?.Invoke(day);
    }

    private void OnDestroy()
    {
        rewardBtn?.onClick.RemoveAllListeners();
    }
}