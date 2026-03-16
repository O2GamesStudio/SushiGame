using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CoinPassView : MonoBehaviour
{
    [SerializeField] private PassDataBase passDataBase;
    [SerializeField] private Transform content;
    [SerializeField] private PassRowView passRowPrefab;

    [Header("Header")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Button buyPassBtn;
    [SerializeField] private GameObject buyPassPanel;
    [SerializeField] private Button confirmBuyPassBtn;
    [SerializeField] private Button cancelBuyPassBtn;

    private List<PassRowView> rows = new List<PassRowView>();

    private void OnEnable()
    {
        buyPassBtn?.onClick.AddListener(OnBuyPassClicked);
        confirmBuyPassBtn?.onClick.AddListener(OnConfirmBuyPassClicked);
        cancelBuyPassBtn?.onClick.AddListener(OnCancelBuyPassClicked);
        IAPManager.Instance.OnPurchaseSuccess += OnPurchaseSuccess;
        Refresh();
    }

    private void OnDisable()
    {
        buyPassBtn?.onClick.RemoveAllListeners();
        confirmBuyPassBtn?.onClick.RemoveAllListeners();
        cancelBuyPassBtn?.onClick.RemoveAllListeners();
        if (IAPManager.Instance != null)
            IAPManager.Instance.OnPurchaseSuccess -= OnPurchaseSuccess;
    }

    public void Initialize()
    {
        foreach (var row in rows)
            if (row != null) Destroy(row.gameObject);
        rows.Clear();

        for (int i = 1; i <= passDataBase.MaxLevel; i++)
        {
            var data = passDataBase.Get(i);
            if (data == null) continue;

            var row = Instantiate(passRowPrefab, content);
            row.Setup(i, data);
            rows.Add(row);
        }

        RefreshProgress();
    }

    public void Refresh()
    {
        RefreshProgress();
        foreach (var row in rows)
            row?.Refresh();
    }

    private void RefreshProgress()
    {
        var manager = CoinPassManager.Instance;
        if (manager == null) return;

        float progress = manager.GetLevelProgress();
        if (progressBar != null) progressBar.value = progress;

        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;

        var levelData = passDataBase.Get(userData.passLevel);
        if (levelData == null) return;

        if (progressText != null)
            progressText.text = $"{userData.passXP}/{levelData.requiredXP}";

        buyPassBtn?.gameObject.SetActive(!manager.HasPass());
    }

    private void OnBuyPassClicked()
    {
        buyPassPanel?.SetActive(true);
    }

    private void OnConfirmBuyPassClicked()
    {
        buyPassPanel?.SetActive(false);
        IAPManager.Instance?.BuyProduct(IAPManager.ItemPackage);
    }

    private void OnCancelBuyPassClicked()
    {
        buyPassPanel?.SetActive(false);
    }

    private void OnPurchaseSuccess(string productId)
    {
        if (productId == IAPManager.ItemPackage)
        {
            CoinPassManager.Instance?.BuyPass();
            Refresh();
        }
    }
}