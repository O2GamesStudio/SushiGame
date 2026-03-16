using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using System;
using System.Collections.Generic;

public class IAPManager : MonoBehaviour, IDetailedStoreListener
{
    public static IAPManager Instance { get; private set; }

    private IStoreController storeController;
    private IExtensionProvider extensionProvider;

    public const string StarterPackage = "starter.package";
    public const string ItemPackage = "item.package";
    public const string Coin400 = "coin.400";
    public const string Coin2200 = "coin.2200";
    public const string Coin11500 = "coin.11500";
    public const string RemoveAds = "remove.ads";

    public event Action<string> OnPurchaseSuccess;
    public event Action<string> OnPurchaseFailedEvent;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializePurchasing();
    }

    private void InitializePurchasing()
    {
        var module = StandardPurchasingModule.Instance();

#if UNITY_EDITOR
        module.useFakeStoreUIMode = FakeStoreUIMode.StandardUser;
#endif

        var builder = ConfigurationBuilder.Instance(module);

        builder.AddProduct(StarterPackage, ProductType.Consumable);
        builder.AddProduct(ItemPackage, ProductType.Consumable);
        builder.AddProduct(Coin400, ProductType.Consumable);
        builder.AddProduct(Coin2200, ProductType.Consumable);
        builder.AddProduct(Coin11500, ProductType.Consumable);
        builder.AddProduct(RemoveAds, ProductType.NonConsumable);

        UnityPurchasing.Initialize(this, builder);
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        extensionProvider = extensions;
    }

    public void OnInitializeFailed(InitializationFailureReason error) { }
    public void OnInitializeFailed(InitializationFailureReason error, string message) { }

    public void BuyProduct(string productId)
    {
        if (storeController == null) return;
        storeController.InitiatePurchase(productId);
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        string productId = args.purchasedProduct.definition.id;
        GrantReward(productId);
        OnPurchaseSuccess?.Invoke(productId);
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        OnPurchaseFailedEvent?.Invoke(product.definition.id);
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        OnPurchaseFailedEvent?.Invoke(product.definition.id);
    }

    private void GrantReward(string productId)
    {
        var userData = GameDataTransfer.Instance?.CurrentUserData;
        if (userData == null) return;

        string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId)) return;

        switch (productId)
        {
            case StarterPackage:
                userData.coin += 500;
                userData.itemRandomRemover += 3;
                break;
            case ItemPackage:
                userData.itemRandomRemover += 2;
                userData.itemTargetRemover += 2;
                userData.itemTimeFreezer += 2;
                userData.itemShuffler += 2;
                break;
            case Coin400:
                userData.coin += 400;
                break;
            case Coin2200:
                userData.coin += 2200;
                break;
            case Coin11500:
                userData.coin += 11500;
                break;
            case RemoveAds:
                userData.isAdsRemoved = true;
                UnityAdsManager.Instance?.HideBanner();
                break;
        }

        GameDataTransfer.Instance.SetUserData(userData);

        var updates = new Dictionary<string, object>
        {
            { "coin", userData.coin },
            { "itemRandomRemover", userData.itemRandomRemover },
            { "itemTargetRemover", userData.itemTargetRemover },
            { "itemTimeFreezer", userData.itemTimeFreezer },
            { "itemShuffler", userData.itemShuffler },
            { "isAdsRemoved", userData.isAdsRemoved }
        };

        UserDataService.Instance?.UpdateFields(userId, updates);
        LobbyUIManager.Instance?.UpdateUI(userData);
    }

    public bool IsProductPurchased(string productId)
    {
        if (storeController == null) return false;
        var product = storeController.products.WithID(productId);
        return product != null && product.hasReceipt;
    }

    public bool IsInitialized() => storeController != null;
}