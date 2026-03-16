using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AddItemPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private Image itemImage;
    [SerializeField] private Button addAdsBtn;
    [SerializeField] private Button addCoinBtn;
    [SerializeField] private Button exitBtn;
    [SerializeField] private TextMeshProUGUI coinCostText;
    [SerializeField] private Sprite[] itemSprites;

    private bool isWaitingForAd = false;
    private const int CoinCost = 100;
    private string currentItemKey;
    private bool isGranted = false;

    private void OnEnable()
    {
        addAdsBtn?.onClick.AddListener(OnAddAdsBtnClicked);
        addCoinBtn?.onClick.AddListener(OnAddCoinBtnClicked);
        exitBtn?.onClick.AddListener(OnExitClicked);
        if (coinCostText != null) coinCostText.text = CoinCost.ToString();
    }

    private void OnDisable()
    {
        addAdsBtn?.onClick.RemoveAllListeners();
        addCoinBtn?.onClick.RemoveAllListeners();
        exitBtn?.onClick.RemoveAllListeners();

        if (UnityAdsManager.Instance != null)
        {
            UnityAdsManager.Instance.OnRewardEarned -= OnAdRewardEarned;
            UnityAdsManager.Instance.OnAdFailedToShow -= OnAdFailed;
        }

        isGranted = false;
    }

    public void Show(string itemName, string firestoreKey, int spriteIndex)
    {
        currentItemKey = firestoreKey;
        isGranted = false;

        if (itemNameText != null) itemNameText.text = itemName;
        if (itemImage != null && spriteIndex < itemSprites.Length)
            itemImage.sprite = itemSprites[spriteIndex];

        gameObject.SetActive(true);
    }

    private void OnAddAdsBtnClicked()
    {
        if (UnityAdsManager.Instance == null) return;
        UnityAdsManager.Instance.OnRewardEarned += OnAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow += OnAdFailed;
        UnityAdsManager.Instance.ShowRewardedAd();
    }

    private void OnAdRewardEarned()
    {
        UnityAdsManager.Instance.OnRewardEarned -= OnAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow -= OnAdFailed;

        if (isGranted) return;
        isGranted = true;

        GrantItem();
        gameObject.SetActive(false);
    }

    private void OnAdFailed()
    {
        UnityAdsManager.Instance.OnRewardEarned -= OnAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow -= OnAdFailed;
    }

    private void OnAddCoinBtnClicked()
    {
        if (isGranted) return;

        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;
        if (userData.coin < CoinCost) return;

        string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId)) return;

        isGranted = true;
        userData.coin -= CoinCost;
        GrantItem(userData, userId);
        gameObject.SetActive(false);
    }

    private void GrantItem(UserData userData = null, string userId = null)
    {
        if (userData == null) userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;
        if (userId == null) userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId)) return;

        int newCount = 0;
        switch (currentItemKey)
        {
            case "itemRandomRemover": userData.itemRandomRemover++; newCount = userData.itemRandomRemover; break;
            case "itemTargetRemover": userData.itemTargetRemover++; newCount = userData.itemTargetRemover; break;
            case "itemTimeFreezer": userData.itemTimeFreezer++; newCount = userData.itemTimeFreezer; break;
            case "itemShuffler": userData.itemShuffler++; newCount = userData.itemShuffler; break;
        }

        var updates = new Dictionary<string, object>
        {
            { "coin", userData.coin },
            { currentItemKey, newCount }
        };

        GameDataTransfer.Instance.SetUserData(userData);
        UserDataService.Instance?.UpdateFields(userId, updates);
        ItemManager.Instance?.InitializeItemCounts(userData);
    }

    private void OnExitClicked() => gameObject.SetActive(false);
}