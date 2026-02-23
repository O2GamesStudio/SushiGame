using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(PolygonCollider2D))]
public class Sushi : MonoBehaviour
{
    [SerializeField] private int typeId = -1;

    [Header("Sushi Parts")]
    [SerializeField] private SpriteRenderer riceRenderer;
    [SerializeField] private SpriteRenderer toppingRenderer;

    [Header("Drag Visual")]
    [SerializeField] private float dragScale = 1.2f;
    [SerializeField] private float outlineThickness = 0.05f;

    [Header("Lock Visual")]
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private Sprite[] lockStageSprites = new Sprite[3];

    public int TypeId => typeId;
    public SpriteRenderer SpriteRenderer => riceRenderer;
    public Transform RicePart => riceRenderer.transform;
    public Transform ToppingPart => toppingRenderer.transform;
    public bool IsLocked => lockStage > 0;
    public int LockStage => lockStage;
    public Plate CurrentPlate { get; private set; }

    private Vector3 originalScale;
    private Material riceMaterialInstance;
    private Material toppingMaterialInstance;
    private int lockStage = 0;
    private SpriteRenderer lockIconRenderer;

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

        if (riceRenderer != null && riceRenderer.material != null)
        {
            riceMaterialInstance = riceRenderer.material;
        }

        if (toppingRenderer != null && toppingRenderer.material != null)
        {
            toppingMaterialInstance = toppingRenderer.material;
        }

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

    public void Initialize(int id, Sprite riceSprite, Sprite toppingSprite, float toppingOffsetY = 0f)
    {
        typeId = id;

        if (riceRenderer != null)
            riceRenderer.sprite = riceSprite;

        if (toppingRenderer != null)
        {
            toppingRenderer.sprite = toppingSprite;
            toppingRenderer.transform.localPosition = new Vector3(0f, toppingOffsetY, 0f);
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
            {
                lockIconRenderer.sprite = lockStageSprites[spriteIndex];
            }
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
            riceRenderer.transform.localScale = Vector3.one;

        if (toppingRenderer != null)
        {
            toppingRenderer.transform.localScale = Vector3.one;
        }

        CurrentPlate = null;
        lockStage = 0;
        if (lockIcon != null)
        {
            lockIcon.SetActive(false);
        }

        if (riceMaterialInstance != null)
        {
            riceMaterialInstance.SetFloat("_OutlineThickness", 0f);
        }

        if (toppingMaterialInstance != null)
        {
            toppingMaterialInstance.SetFloat("_OutlineThickness", 0f);
        }
    }

    public void SetDragState(bool isDragging)
    {
        if (isDragging)
        {
            transform.DOScale(originalScale * dragScale, 0.2f).SetEase(Ease.OutBack);
            if (riceMaterialInstance != null)
            {
                riceMaterialInstance.SetFloat("_OutlineThickness", outlineThickness);
            }
            if (toppingMaterialInstance != null)
            {
                toppingMaterialInstance.SetFloat("_OutlineThickness", outlineThickness);
            }
        }
        else
        {
            transform.DOScale(originalScale, 0.2f).SetEase(Ease.OutQuad);
            if (riceMaterialInstance != null)
            {
                riceMaterialInstance.SetFloat("_OutlineThickness", 0f);
            }
            if (toppingMaterialInstance != null)
            {
                toppingMaterialInstance.SetFloat("_OutlineThickness", 0f);
            }
        }
    }
}