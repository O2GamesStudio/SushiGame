using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer), typeof(PolygonCollider2D))]
public class Sushi : MonoBehaviour
{
    [SerializeField] private int typeId = -1;
    [Header("Drag Visual")]
    [SerializeField] private float dragScale = 1.2f;
    [SerializeField] private float outlineThickness = 0.05f;

    [Header("Lock Visual")]
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private Sprite[] lockStageSprites = new Sprite[3];

    public int TypeId => typeId;
    public SpriteRenderer SpriteRenderer { get; private set; }
    public bool IsLocked => lockStage > 0;
    public int LockStage => lockStage;
    public Plate CurrentPlate { get; private set; }

    private Vector3 originalScale;
    private Material materialInstance;
    private int lockStage = 0;
    private SpriteRenderer lockIconRenderer;

    private void Awake()
    {
        SpriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;

        if (SpriteRenderer.material != null)
        {
            materialInstance = SpriteRenderer.material;
        }

        if (lockIcon != null)
        {
            lockIconRenderer = lockIcon.GetComponent<SpriteRenderer>();
            lockIcon.SetActive(false);
        }
    }

    public void Initialize(int id, Sprite sprite)
    {
        typeId = id;
        SpriteRenderer.sprite = sprite;
        gameObject.name = $"Sushi_{id}";

        lockStage = 0;
        if (lockIcon != null)
        {
            lockIcon.SetActive(false);
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

        CurrentPlate = null;
        lockStage = 0;
        if (lockIcon != null)
        {
            lockIcon.SetActive(false);
        }

        if (materialInstance != null)
        {
            materialInstance.SetFloat("_OutlineThickness", 0f);
        }
    }

    public void SetDragState(bool isDragging)
    {
        if (isDragging)
        {
            transform.DOScale(originalScale * dragScale, 0.2f).SetEase(Ease.OutBack);
            if (materialInstance != null)
            {
                materialInstance.SetFloat("_OutlineThickness", outlineThickness);
            }
        }
        else
        {
            transform.DOScale(originalScale, 0.2f).SetEase(Ease.OutQuad);
            if (materialInstance != null)
            {
                materialInstance.SetFloat("_OutlineThickness", 0f);
            }
        }
    }
}