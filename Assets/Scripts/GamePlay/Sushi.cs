using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class Sushi : MonoBehaviour
{
    [SerializeField] private int typeId = -1;
    [Header("Drag Visual")]
    [SerializeField] private float dragScale = 1.2f;
    [SerializeField] private float outlineThickness = 0.05f;

    public int TypeId => typeId;
    public SpriteRenderer SpriteRenderer { get; private set; }

    private Vector3 originalScale;
    private Material materialInstance;

    private void Awake()
    {
        SpriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;

        if (SpriteRenderer.material != null)
        {
            materialInstance = SpriteRenderer.material;
        }
    }

    public void Initialize(int id, Sprite sprite)
    {
        typeId = id;
        SpriteRenderer.sprite = sprite;
        gameObject.name = $"Sushi_{id}";
    }

    public void Reset()
    {
        typeId = -1;
        transform.position = Vector3.zero;
        transform.localScale = Vector3.one;
        originalScale = Vector3.one;
        gameObject.name = "Sushi_Reset";

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