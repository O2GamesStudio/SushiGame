using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(PolygonCollider2D))]
public class Sushi : MonoBehaviour
{
    [SerializeField] private int typeId = -1;
    public SushiType SushiType { get; private set; }

    [Header("Sushi Parts")]
    [SerializeField] private SpriteRenderer riceRenderer;
    [SerializeField] private SpriteRenderer toppingRenderer;

    [Header("Drag Visual")]
    [SerializeField] private float dragScale = 1.2f;
    [SerializeField] private float outlineThickness = 0.05f;

    [Header("Lock Visual")]
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private Sprite[] lockStageSprites = new Sprite[3];

    [Header("Type Change VFX")]
    [SerializeField] private ParticleSystem typeChangeVFXPrefab;

    public int TypeId => typeId;
    public SpriteRenderer SpriteRenderer => riceRenderer;
    public Transform RicePart => riceRenderer.transform;
    public Transform ToppingPart => toppingRenderer != null ? toppingRenderer.transform : null;
    public bool IsLocked => lockStage > 0;
    public int LockStage => lockStage;
    public bool IsHidden => isHidden;
    public Plate CurrentPlate { get; private set; }
    public bool IsDragging { get; private set; }
    public float PlateOffsetY { get; private set; }

    private Vector3 originalScale;
    private Material riceMaterialInstance;
    private Material toppingMaterialInstance;
    private int lockStage = 0;
    private SpriteRenderer lockIconRenderer;
    private int originalRiceSortingOrder;
    private bool isHidden = false;
    private GameObject hiddenOverlay;
    private SpriteRenderer hiddenOverlayRenderer;

    private void Awake()
    {
        if (riceRenderer == null)
        {
            var ricePart = transform.Find("RicePart");
            if (ricePart != null)
                riceRenderer = ricePart.GetComponent<SpriteRenderer>();
        }

        if (toppingRenderer == null)
        {
            var toppingPart = transform.Find("ToppingPart");
            if (toppingPart != null)
                toppingRenderer = toppingPart.GetComponent<SpriteRenderer>();
        }

        originalScale = transform.localScale;

        if (riceRenderer != null)
        {
            riceMaterialInstance = riceRenderer.material;
            originalRiceSortingOrder = riceRenderer.sortingOrder;
        }

        if (toppingRenderer != null)
            toppingMaterialInstance = toppingRenderer.material;

        if (lockIcon != null)
        {
            lockIconRenderer = lockIcon.GetComponent<SpriteRenderer>();
            if (lockIconRenderer != null)
            {
                lockIconRenderer.sortingLayerName = riceRenderer.sortingLayerName;
                lockIconRenderer.sortingOrder = riceRenderer.sortingOrder + 2;
            }
            lockIcon.SetActive(false);
        }
    }

    public void Initialize(int id, Sprite riceSprite, Sprite toppingSprite,
        SushiType sushiType = SushiType.Nigiri, float toppingOffsetX = 0f,
        float toppingOffsetY = 0f, float plateOffsetY = 0f)
    {
        typeId = id;
        SushiType = sushiType;
        PlateOffsetY = plateOffsetY;

        if (riceRenderer != null)
        {
            riceRenderer.sprite = riceSprite;
            riceRenderer.transform.localPosition = Vector3.zero;
        }

        if (toppingRenderer != null)
        {
            toppingRenderer.sprite = toppingSprite;
            toppingRenderer.gameObject.SetActive(sushiType != SushiType.Integrated && toppingSprite != null);
            toppingRenderer.transform.localPosition = new Vector3(toppingOffsetX, toppingOffsetY, 0f);
        }

        gameObject.name = $"Sushi_{id}";
        lockStage = 0;
        if (lockIcon != null)
            lockIcon.SetActive(false);

        EnforceSortingOrder();
    }

    public void EnforceSortingOrder()
    {
        if (riceRenderer == null) return;
        int baseOrder = riceRenderer.sortingOrder;

        if (toppingRenderer != null)
        {
            toppingRenderer.sortingLayerName = riceRenderer.sortingLayerName;
            toppingRenderer.sortingOrder = baseOrder + 1;
        }

        if (lockIconRenderer != null)
        {
            lockIconRenderer.sortingLayerName = riceRenderer.sortingLayerName;
            lockIconRenderer.sortingOrder = baseOrder + 2;
        }

        if (hiddenOverlayRenderer != null)
        {
            hiddenOverlayRenderer.sortingLayerName = riceRenderer.sortingLayerName;
            hiddenOverlayRenderer.sortingOrder = baseOrder + 3;
        }
    }

    public void SetBaseSortingOrder(string layerName, int baseOrder)
    {
        if (riceRenderer != null)
        {
            riceRenderer.sortingLayerName = layerName;
            riceRenderer.sortingOrder = baseOrder;
        }

        if (toppingRenderer != null)
        {
            toppingRenderer.sortingLayerName = layerName;
            toppingRenderer.sortingOrder = baseOrder + 1;
        }

        if (lockIconRenderer != null)
        {
            lockIconRenderer.sortingLayerName = layerName;
            lockIconRenderer.sortingOrder = baseOrder + 2;
        }

        if (hiddenOverlayRenderer != null)
        {
            hiddenOverlayRenderer.sortingLayerName = layerName;
            hiddenOverlayRenderer.sortingOrder = baseOrder + 3;
        }
    }

    public void SetHidden(bool hidden, Sprite hiddenSprite = null)
    {
        isHidden = hidden;

        if (hidden)
        {
            if (riceRenderer != null) riceRenderer.enabled = false;
            if (toppingRenderer != null) toppingRenderer.enabled = false;

            if (hiddenOverlay == null)
            {
                hiddenOverlay = new GameObject("HiddenOverlay");
                hiddenOverlay.transform.SetParent(transform);
                hiddenOverlay.transform.localPosition = Vector3.zero;
                hiddenOverlay.transform.localScale = Vector3.one * 1.5f;
                hiddenOverlayRenderer = hiddenOverlay.AddComponent<SpriteRenderer>();
                hiddenOverlayRenderer.sortingLayerName = riceRenderer.sortingLayerName;
                hiddenOverlayRenderer.sortingOrder = riceRenderer.sortingOrder + 3;
            }

            if (hiddenSprite != null && hiddenOverlayRenderer != null)
                hiddenOverlayRenderer.sprite = hiddenSprite;

            hiddenOverlay.SetActive(true);
        }
        else
        {
            if (hiddenOverlay != null)
                hiddenOverlay.SetActive(false);

            if (riceRenderer != null) riceRenderer.enabled = true;
            if (toppingRenderer != null) toppingRenderer.enabled = true;
        }
    }

    public void SetRenderersVisible(bool visible)
    {
        if (riceRenderer != null) riceRenderer.enabled = visible;
        if (toppingRenderer != null) toppingRenderer.enabled = visible;
    }

    public void RevealHidden(System.Action onComplete = null)
    {
        if (!isHidden || hiddenOverlay == null)
        {
            onComplete?.Invoke();
            return;
        }

        isHidden = false;

        if (riceRenderer != null) riceRenderer.enabled = true;
        if (toppingRenderer != null) toppingRenderer.enabled = true;

        var overlayToDestroy = hiddenOverlay;
        var rendererToFade = hiddenOverlayRenderer;
        hiddenOverlay = null;
        hiddenOverlayRenderer = null;

        rendererToFade?.DOFade(0f, 0.3f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                if (overlayToDestroy != null)
                    Destroy(overlayToDestroy);
                onComplete?.Invoke();
            });
    }

    public void ShowLockIcon(Sprite sprite)
    {
        if (lockIcon == null || lockIconRenderer == null) return;
        lockIcon.SetActive(true);
        lockIconRenderer.sprite = sprite;
    }

    public void HideDecoratorSprites()
    {
        if (lockIcon != null)
            lockIcon.SetActive(false);

        if (hiddenOverlay != null)
        {
            Destroy(hiddenOverlay);
            hiddenOverlay = null;
            hiddenOverlayRenderer = null;
        }

        isHidden = false;
        if (riceRenderer != null) riceRenderer.enabled = true;
        if (toppingRenderer != null) toppingRenderer.enabled = true;
    }

    public void PlayShuffleAnimation(int newTypeId, Sprite newRiceSprite, Sprite newToppingSprite,
        SushiType newSushiType, float newToppingOffsetX, float newToppingOffsetY, float newPlateOffsetY,
        System.Action onComplete)
    {
        bool currentIsIntegrated = SushiType == SushiType.Integrated;
        bool nextIsIntegrated = newSushiType == SushiType.Integrated;

        if (!currentIsIntegrated && nextIsIntegrated)
            PlayNormalToIntegratedAnimation(newTypeId, newRiceSprite, newSushiType, newPlateOffsetY, onComplete);
        else if (currentIsIntegrated && !nextIsIntegrated)
            PlayIntegratedToNormalAnimation(newTypeId, newRiceSprite, newToppingSprite, newSushiType, newToppingOffsetX, newToppingOffsetY, newPlateOffsetY, onComplete);
        else if (currentIsIntegrated)
            PlayIntegratedToIntegratedAnimation(newTypeId, newRiceSprite, newSushiType, newPlateOffsetY, onComplete);
        else
            PlayToppingAnimation(newTypeId, newRiceSprite, newToppingSprite, newSushiType, newToppingOffsetX, newToppingOffsetY, newPlateOffsetY, onComplete);
    }

    private void PlayNormalToIntegratedAnimation(int newTypeId, Sprite newRiceSprite,
    SushiType newSushiType, float newPlateOffsetY, System.Action onComplete)
    {
        ApplyIntegratedState(newTypeId, newRiceSprite, newSushiType, newPlateOffsetY);
        SpawnTypeChangeVFX();
        onComplete?.Invoke();
    }

    private void PlayIntegratedToNormalAnimation(int newTypeId, Sprite newRiceSprite, Sprite newToppingSprite,
        SushiType newSushiType, float newToppingOffsetX, float newToppingOffsetY, float newPlateOffsetY,
        System.Action onComplete)
    {
        ApplyNormalState(newTypeId, newRiceSprite, newToppingSprite, newSushiType, newToppingOffsetX, newToppingOffsetY, newPlateOffsetY);
        SpawnTypeChangeVFX();
        onComplete?.Invoke();
    }

    private void SpawnTypeChangeVFX()
    {
        if (typeChangeVFXPrefab == null) return;
        var vfx = Instantiate(typeChangeVFXPrefab, transform.position, Quaternion.identity);
        vfx.Play();
        Destroy(vfx.gameObject, vfx.main.duration + vfx.main.startLifetime.constantMax);
    }
    private void PlayIntegratedToIntegratedAnimation(int newTypeId, Sprite newRiceSprite,
        SushiType newSushiType, float newPlateOffsetY, System.Action onComplete)
    {
        if (RicePart == null)
        {
            ApplyIntegratedState(newTypeId, newRiceSprite, newSushiType, newPlateOffsetY);
            onComplete?.Invoke();
            return;
        }

        Sequence seq = DOTween.Sequence();
        seq.Append(RicePart.DORotate(new Vector3(0f, 90f, 0f), 0.2f, RotateMode.Fast)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                if (riceRenderer != null) riceRenderer.sprite = newRiceSprite;
            }));
        seq.Append(RicePart.DORotate(Vector3.zero, 0.2f, RotateMode.Fast).SetEase(Ease.OutQuad));

        seq.OnComplete(() =>
        {
            ApplyIntegratedState(newTypeId, newRiceSprite, newSushiType, newPlateOffsetY);
            onComplete?.Invoke();
        });
    }

    private void ApplyIntegratedState(int newTypeId, Sprite newRiceSprite, SushiType newSushiType, float newPlateOffsetY)
    {
        typeId = newTypeId;
        SushiType = newSushiType;
        PlateOffsetY = newPlateOffsetY;
        gameObject.name = $"Sushi_{newTypeId}";
        if (riceRenderer != null) riceRenderer.sprite = newRiceSprite;
        if (toppingRenderer != null) toppingRenderer.gameObject.SetActive(false);
        if (RicePart != null)
            RicePart.localPosition = new Vector3(RicePart.localPosition.x, 0f, RicePart.localPosition.z);
        transform.localPosition = new Vector3(transform.localPosition.x, 0.08f + newPlateOffsetY, transform.localPosition.z);
    }

    private void ApplyNormalState(int newTypeId, Sprite newRiceSprite, Sprite newToppingSprite,
        SushiType newSushiType, float newToppingOffsetX, float newToppingOffsetY, float newPlateOffsetY)
    {
        typeId = newTypeId;
        SushiType = newSushiType;
        PlateOffsetY = newPlateOffsetY;
        gameObject.name = $"Sushi_{newTypeId}";
        if (riceRenderer != null) riceRenderer.sprite = newRiceSprite;
        if (toppingRenderer != null)
        {
            toppingRenderer.sprite = newToppingSprite;
            toppingRenderer.gameObject.SetActive(newToppingSprite != null);
            toppingRenderer.transform.localPosition = new Vector3(newToppingOffsetX, newToppingOffsetY, 0f);
        }
        if (RicePart != null)
            RicePart.localPosition = new Vector3(RicePart.localPosition.x, 0f, RicePart.localPosition.z);
        transform.localPosition = new Vector3(transform.localPosition.x, 0.08f + newPlateOffsetY, transform.localPosition.z);
    }

    private void PlayToppingAnimation(int newTypeId, Sprite newRiceSprite, Sprite newToppingSprite,
        SushiType newSushiType, float newToppingOffsetX, float newToppingOffsetY, float newPlateOffsetY,
        System.Action onComplete)
    {
        if (ToppingPart == null)
        {
            Initialize(newTypeId, newRiceSprite, newToppingSprite, newSushiType, newToppingOffsetX, newToppingOffsetY, newPlateOffsetY);
            onComplete?.Invoke();
            return;
        }

        Vector3 originalPos = ToppingPart.localPosition;

        Sequence seq = DOTween.Sequence();
        seq.Append(ToppingPart.DOLocalMoveY(originalPos.y + 0.5f, 0.2f).SetEase(Ease.OutQuad));
        seq.Append(ToppingPart.DORotate(new Vector3(0f, 90f, 0f), 0.2f, RotateMode.Fast)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                if (riceRenderer != null) riceRenderer.sprite = newRiceSprite;
                if (toppingRenderer != null)
                {
                    bool hasTopping = newToppingSprite != null;
                    toppingRenderer.gameObject.SetActive(hasTopping);
                    if (hasTopping) toppingRenderer.sprite = newToppingSprite;
                }
            }));
        seq.Append(ToppingPart.DORotate(Vector3.zero, 0.2f, RotateMode.Fast).SetEase(Ease.OutQuad));
        seq.Append(ToppingPart.DOLocalMove(new Vector3(newToppingOffsetX, newToppingOffsetY, originalPos.z), 0.25f).SetEase(Ease.OutBounce));
        seq.OnComplete(() =>
        {
            typeId = newTypeId;
            SushiType = newSushiType;
            PlateOffsetY = newPlateOffsetY;
            gameObject.name = $"Sushi_{newTypeId}";
            onComplete?.Invoke();
        });
    }

    public void SetCurrentPlate(Plate plate)
    {
        CurrentPlate = plate;
    }

    public void SetLockStage(int stage)
    {
        lockStage = Mathf.Clamp(stage, 0, 3);
        UpdateLockVisual();
    }

    public void DecreaseLockStage()
    {
        if (lockStage > 0)
        {
            lockStage--;
            UpdateLockVisual();
        }
    }

    private void UpdateLockVisual()
    {
        if (lockIcon == null || lockIconRenderer == null) return;

        if (lockStage > 0)
        {
            lockIcon.SetActive(true);
            lockIconRenderer.sortingLayerName = riceRenderer.sortingLayerName;
            lockIconRenderer.sortingOrder = riceRenderer.sortingOrder + 2;

            int spriteIndex = lockStage - 1;
            if (spriteIndex >= 0 && spriteIndex < lockStageSprites.Length)
                lockIconRenderer.sprite = lockStageSprites[spriteIndex];
        }
        else
        {
            lockIcon.SetActive(false);
        }
    }

    public void Reset()
    {
        typeId = -1;
        transform.position = Vector3.zero;
        transform.localScale = Vector3.one;
        originalScale = Vector3.one;
        gameObject.name = "Sushi_Reset";

        if (riceRenderer != null)
        {
            riceRenderer.transform.localPosition = Vector3.zero;
            riceRenderer.transform.localScale = Vector3.one;
            riceRenderer.sortingOrder = originalRiceSortingOrder;
            riceRenderer.enabled = true;
        }

        if (toppingRenderer != null)
        {
            toppingRenderer.transform.localScale = Vector3.one;
            toppingRenderer.enabled = true;
            toppingRenderer.gameObject.SetActive(true);
        }

        isHidden = false;
        if (hiddenOverlay != null)
        {
            Destroy(hiddenOverlay);
            hiddenOverlay = null;
            hiddenOverlayRenderer = null;
        }

        CurrentPlate = null;
        lockStage = 0;
        IsDragging = false;

        if (lockIcon != null)
            lockIcon.SetActive(false);

        if (riceMaterialInstance != null)
            riceMaterialInstance.SetFloat("_OutlineThickness", 0f);

        if (toppingMaterialInstance != null)
            toppingMaterialInstance.SetFloat("_OutlineThickness", 0f);
    }

    public void SetDragState(bool isDragging)
    {
        IsDragging = isDragging;

        if (isDragging)
        {
            transform.DOScale(originalScale * dragScale, 0.2f).SetEase(Ease.OutBack);

            if (riceRenderer != null)
                riceRenderer.sortingOrder = 100;
            if (toppingRenderer != null)
                toppingRenderer.sortingOrder = 101;
            if (lockIconRenderer != null)
                lockIconRenderer.sortingOrder = 102;

            if (riceMaterialInstance != null)
                riceMaterialInstance.SetFloat("_OutlineThickness", outlineThickness);
            if (toppingMaterialInstance != null)
                toppingMaterialInstance.SetFloat("_OutlineThickness", outlineThickness);
        }
        else
        {
            transform.DOScale(originalScale, 0.2f).SetEase(Ease.OutQuad);

            if (riceRenderer != null)
                riceRenderer.sortingOrder = originalRiceSortingOrder;

            EnforceSortingOrder();

            if (riceMaterialInstance != null)
                riceMaterialInstance.SetFloat("_OutlineThickness", 0f);
            if (toppingMaterialInstance != null)
                toppingMaterialInstance.SetFloat("_OutlineThickness", 0f);
        }
    }
}