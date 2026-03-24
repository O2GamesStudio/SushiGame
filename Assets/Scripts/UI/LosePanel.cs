using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

public class LosePanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button retryBtn;
    [SerializeField] private Button exitLobbyBtn;
    [SerializeField] private Image headImage;

    [Header("Stamina")]
    [SerializeField] private TextMeshProUGUI staminaText;
    [SerializeField] private TextMeshProUGUI staminaChargingText;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private Button staminaButton;
    [SerializeField] private GameObject addStaminaPanel;

    [Header("Animation")]
    [SerializeField] private Image[] loseImages;
    [SerializeField] private Image[] loseCircleImages;

    private Coroutine staminaChargeCoroutine;

    private void OnEnable()
    {
        retryBtn?.onClick.AddListener(OnRetryButtonClicked);
        exitLobbyBtn?.onClick.AddListener(() => SceneLoader.LoadLobby());
        staminaButton?.onClick.AddListener(() => addStaminaPanel?.SetActive(true));
        PlayHeadAnimation();
        PlayLoseImagesAnimation();
        PlayCircleAnimation();
    }

    private void OnDisable()
    {
        retryBtn?.onClick.RemoveAllListeners();
        exitLobbyBtn?.onClick.RemoveAllListeners();
        staminaButton?.onClick.RemoveAllListeners();

        headImage?.rectTransform.DOKill();

        foreach (var img in loseImages)
            img?.rectTransform.DOKill();
        foreach (var img in loseCircleImages)
            img?.rectTransform.DOKill();
    }

    public void Show()
    {
        gameObject.SetActive(true);

        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;

        if (staminaText != null) staminaText.text = userData.stamina.ToString();
        if (coinText != null) coinText.text = userData.coin.ToString();
        StartStaminaChargeDisplay(userData);
    }

    public void RefreshStaminaUI(UserData userData)
    {
        if (staminaText != null) staminaText.text = userData.stamina.ToString();
    }

    private void PlayHeadAnimation()
    {
        if (headImage == null) return;

        var rt = headImage.rectTransform;
        rt.DOKill();
        Vector2 originPos = rt.anchoredPosition;

        rt.DOAnchorPosY(originPos.y + 20f, 0.6f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(headImage.gameObject);
    }

    private void PlayLoseImagesAnimation()
    {
        foreach (var img in loseImages)
        {
            if (img == null) continue;
            var rt = img.rectTransform;
            rt.DOKill();
            Vector2 originPos = rt.anchoredPosition;

            rt.DOAnchorPosY(originPos.y - 15f, 1.2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(img.gameObject);
        }
    }

    private void PlayCircleAnimation()
    {
        for (int i = 0; i < loseCircleImages.Length; i++)
        {
            var img = loseCircleImages[i];
            if (img == null) continue;
            var rt = img.rectTransform;
            rt.DOKill();

            float direction = i % 2 == 0 ? 360f : -360f;
            rt.DORotate(new Vector3(0f, 0f, direction), 4f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetLink(img.gameObject);
        }
    }

    private void OnRetryButtonClicked()
    {
        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;

        if (userData.stamina < 1)
        {
            addStaminaPanel?.SetActive(true);
            return;
        }

        SceneLoader.LoadGameAsync();
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