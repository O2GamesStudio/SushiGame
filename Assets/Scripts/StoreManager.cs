using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class StoreManager : MonoBehaviour
{
    [SerializeField] private Button toLobbyBtn;
    [SerializeField] private float moveAmount = 15f;
    [SerializeField] private float duration = 0.4f;

    [Header("IAP Buttons")]
    [SerializeField] private Button starterPackageBtn;
    [SerializeField] private Button itemPackageBtn;
    [SerializeField] private Button coin400Btn;
    [SerializeField] private Button coin2200Btn;
    [SerializeField] private Button coin11500Btn;

    [Header("Starter Package")]
    [SerializeField] private GameObject starter1;
    [SerializeField] private GameObject starter2;
    [SerializeField] private GameObject starter3;
    [SerializeField] private GameObject starter4;
    [SerializeField] private GameObject starterCheckPanel;

    private RectTransform rt;
    private RectTransform lobbyBtnRT;
    private Vector2 originalPos;
    private Vector3 originalScale;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        lobbyBtnRT = toLobbyBtn.GetComponent<RectTransform>();
        originalPos = lobbyBtnRT.anchoredPosition;
        originalScale = lobbyBtnRT.localScale;
        toLobbyBtn?.onClick.AddListener(Close);

        starterPackageBtn?.onClick.AddListener(() => IAPManager.Instance?.BuyProduct(IAPManager.StarterPackage));
        itemPackageBtn?.onClick.AddListener(() => IAPManager.Instance?.BuyProduct(IAPManager.ItemPackage));
        coin400Btn?.onClick.AddListener(() => IAPManager.Instance?.BuyProduct(IAPManager.Coin400));
        coin2200Btn?.onClick.AddListener(() => IAPManager.Instance?.BuyProduct(IAPManager.Coin2200));
        coin11500Btn?.onClick.AddListener(() => IAPManager.Instance?.BuyProduct(IAPManager.Coin11500));
    }

    private void OnEnable()
    {
        IAPManager.Instance.OnPurchaseSuccess += OnPurchaseSuccess;
        RefreshStarterPackage();
    }

    private void OnDisable()
    {
        if (IAPManager.Instance != null)
            IAPManager.Instance.OnPurchaseSuccess -= OnPurchaseSuccess;
        lobbyBtnRT.DOKill();
        lobbyBtnRT.anchoredPosition = originalPos;
        lobbyBtnRT.localScale = originalScale;
    }

    private void OnDestroy()
    {
        toLobbyBtn?.onClick.RemoveAllListeners();
        starterPackageBtn?.onClick.RemoveAllListeners();
        itemPackageBtn?.onClick.RemoveAllListeners();
        coin400Btn?.onClick.RemoveAllListeners();
        coin2200Btn?.onClick.RemoveAllListeners();
        coin11500Btn?.onClick.RemoveAllListeners();
    }

    private void OnPurchaseSuccess(string productId)
    {
        if (productId == IAPManager.StarterPackage)
            RefreshStarterPackage();
    }

    private void RefreshStarterPackage()
    {
        bool isPurchased = IAPManager.Instance?.IsProductPurchased(IAPManager.StarterPackage) ?? false;

        starter1?.SetActive(!isPurchased);
        starter2?.SetActive(!isPurchased);
        starter3?.SetActive(!isPurchased);
        starter4?.SetActive(!isPurchased);
        starterCheckPanel?.SetActive(isPurchased);
        if (starterPackageBtn != null)
            starterPackageBtn.interactable = !isPurchased;
    }

    public void Close()
    {
        rt.DOKill();
        rt.DOAnchorPosX(-1150f, 0.4f)
            .SetEase(Ease.InQuad);
    }
}