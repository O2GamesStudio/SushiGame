using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

public class WinPanel : MonoBehaviour
{
    [SerializeField] private GameObject[] sushiToppings;

    [Header("UI")]
    [SerializeField] private Button coin2xBtn;
    [SerializeField] private Button coin1xBtn;
    [SerializeField] private Button exitLobbyBtn;

    [Header("Stamina")]
    [SerializeField] private TextMeshProUGUI staminaText;
    [SerializeField] private TextMeshProUGUI staminaChargingText;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private Button staminaButton;
    [SerializeField] private GameObject addStaminaPanel;

    private Coroutine staminaChargeCoroutine;

    private void OnEnable()
    {
        coin1xBtn.onClick.AddListener(() => ClaimCoinAndNextStage(5));
        coin2xBtn.onClick.AddListener(OnCoin2xButtonClicked);
        exitLobbyBtn.onClick.AddListener(OnExitLobbyButtonClicked);
        staminaButton?.onClick.AddListener(() => addStaminaPanel?.SetActive(true));
    }


    private void OnDisable()
    {
        foreach (var topping in sushiToppings)
        {
            if (topping == null) continue;
            var rt = topping.GetComponent<RectTransform>();
            rt?.DOKill();
        }

        coin1xBtn.onClick.RemoveAllListeners();
        coin2xBtn.onClick.RemoveAllListeners();
        exitLobbyBtn.onClick.RemoveAllListeners();
        staminaButton?.onClick.RemoveAllListeners();

        if (UnityAdsManager.Instance != null)
        {
            UnityAdsManager.Instance.OnRewardEarned -= OnCoin2xAdRewardEarned;
            UnityAdsManager.Instance.OnAdFailedToShow -= OnCoin2xAdFailed;
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
        PlaySushiToppingAnimation();

        coin1xBtn.gameObject.SetActive(false);
        exitLobbyBtn.gameObject.SetActive(false);

        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;

        if (staminaText != null) staminaText.text = userData.stamina.ToString();
        if (coinText != null) coinText.text = userData.coin.ToString();
        StartStaminaChargeDisplay(userData);

        StartCoroutine(ShowButtonsCoroutine());
    }
    private IEnumerator ShowButtonsCoroutine()
    {
        yield return new WaitForSeconds(2f);

        ShowButtonWithAnimation(coin1xBtn.gameObject);

        yield return new WaitForSeconds(0.3f);

        ShowButtonWithAnimation(exitLobbyBtn.gameObject);
    }

    private void ShowButtonWithAnimation(GameObject btnObj)
    {
        btnObj.SetActive(true);
        btnObj.transform.localScale = Vector3.zero;
        btnObj.transform.DOScale(1.1f, 0.2f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
                btnObj.transform.DOScale(1f, 0.1f).SetEase(Ease.InQuad));
    }
    private void PlaySushiToppingAnimation()
    {
        foreach (var topping in sushiToppings)
        {
            if (topping == null) continue;

            var rt = topping.GetComponent<RectTransform>();
            if (rt == null) continue;

            rt.DOKill();
            Vector2 originPos = rt.anchoredPosition;

            DOTween.Sequence()
                .Append(rt.DOAnchorPosY(originPos.y + 80f, 0.2f).SetEase(Ease.OutQuad))
                .Append(rt.DOShakeAnchorPos(0.3f, new Vector2(8f, 4f), 15, 0f, false))
                .Append(rt.DOAnchorPos(originPos, 0.3f).SetEase(Ease.OutBounce))
                .AppendInterval(0.3f)
                .SetLoops(-1, LoopType.Restart)
                .SetLink(topping);
        }
    }


    public void RefreshStaminaUI(UserData userData)
    {
        if (staminaText != null) staminaText.text = userData.stamina.ToString();
    }

    private void ClaimCoinAndNextStage(int coinAmount)
    {
        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;

        string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId)) return;

        userData.coin += coinAmount;
        GameDataTransfer.Instance.SetUserData(userData);

        UserDataService.Instance?.UpdateCurrency(userId, userData.stamina, userData.coin, () =>
        {
            LoadNextStage();
        });
    }

    private void LoadNextStage()
    {
        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;

        var levelDataBase = Resources.Load<LevelDataBase>("LevelDataBase");
        var nextLevelData = levelDataBase?.Get(userData.currentStage);
        if (nextLevelData == null)
        {
            SceneLoader.LoadLobby();
            return;
        }

        GameDataTransfer.Instance.SetLevelData(nextLevelData);
        SceneLoader.LoadGameAsync(LoadingUI.Instance);
    }

    private void OnExitLobbyButtonClicked()
    {
        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) { SceneLoader.LoadLobby(); return; }

        string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId)) { SceneLoader.LoadLobby(); return; }

        userData.coin += 5;
        GameDataTransfer.Instance.SetUserData(userData);

        UserDataService.Instance?.UpdateCurrency(userId, userData.stamina, userData.coin, () =>
        {
            SceneLoader.LoadLobby();
        });
    }

    private void OnCoin2xButtonClicked()
    {
        if (UnityAdsManager.Instance == null) return;
        UnityAdsManager.Instance.OnRewardEarned += OnCoin2xAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow += OnCoin2xAdFailed;
        UnityAdsManager.Instance.ShowRewardedAd();
    }

    private void OnCoin2xAdRewardEarned()
    {
        UnityAdsManager.Instance.OnRewardEarned -= OnCoin2xAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow -= OnCoin2xAdFailed;
        ClaimCoinAndNextStage(20);
    }

    private void OnCoin2xAdFailed()
    {
        UnityAdsManager.Instance.OnRewardEarned -= OnCoin2xAdRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow -= OnCoin2xAdFailed;
    }

    private void StartStaminaChargeDisplay(UserData userData)
    {
        if (staminaChargingText == null) return;

        if (userData.stamina >= StaminaChargeCalculator.MaxStamina)
        {
            staminaChargingText.gameObject.SetActive(false);
            return;
        }

        var (_, _, remainingSeconds) = StaminaChargeCalculator.Calculate(userData.stamina, userData.staminaLastChargeTime);
        staminaChargingText.gameObject.SetActive(true);

        if (staminaChargeCoroutine != null) StopCoroutine(staminaChargeCoroutine);
        staminaChargeCoroutine = StartCoroutine(StaminaChargeCoroutine(remainingSeconds));
    }

    private IEnumerator StaminaChargeCoroutine(float remainingSeconds)
    {
        float timeLeft = remainingSeconds;
        while (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            int s = Mathf.Max(0, Mathf.CeilToInt(timeLeft));
            if (staminaChargingText != null)
                staminaChargingText.text = $"{s / 60}m {s % 60}s";
            yield return null;
        }
    }
}