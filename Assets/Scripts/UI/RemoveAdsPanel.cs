using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class RemoveAdsPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelObject;
    [SerializeField] private Button exitBtn;
    [SerializeField] private Button removeAdsBtn;
    [SerializeField] private TextMeshProUGUI removeAdsAmountText;
    [SerializeField] private GameObject removeAdsBuyImage;

    private void OnEnable()
    {
        exitBtn?.onClick.AddListener(OnExitClicked);
        removeAdsBtn?.onClick.AddListener(OnRemoveAdsClicked);
        IAPManager.Instance.OnPurchaseSuccess += OnPurchaseSuccess;
        PlayShowAnimation();
        Refresh();
    }

    private void OnDisable()
    {
        exitBtn?.onClick.RemoveAllListeners();
        removeAdsBtn?.onClick.RemoveAllListeners();
        if (IAPManager.Instance != null)
            IAPManager.Instance.OnPurchaseSuccess -= OnPurchaseSuccess;
        panelObject.transform.DOKill();
    }

    private void PlayShowAnimation()
    {
        panelObject.transform.localScale = Vector3.zero;
        panelObject.transform.DOScale(1.2f, 0.2f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
                panelObject.transform.DOScale(1f, 0.1f).SetEase(Ease.InQuad));
    }

    private void Refresh()
    {
        bool isRemoved = GameDataTransfer.Instance?.CurrentUserData?.isAdsRemoved ?? false;

        if (removeAdsAmountText != null)
            removeAdsAmountText.text = isRemoved ? "" : "5,900원";

        removeAdsBuyImage?.SetActive(isRemoved);
        removeAdsBtn.interactable = !isRemoved;
    }

    private void OnPurchaseSuccess(string productId)
    {
        if (productId == IAPManager.RemoveAds)
            Refresh();
    }

    private void OnRemoveAdsClicked()
    {
        IAPManager.Instance?.BuyProduct(IAPManager.RemoveAds);
    }

    private void OnExitClicked() => gameObject.SetActive(false);
}