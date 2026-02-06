using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class Sushi : MonoBehaviour
{
    [SerializeField] private int typeId = -1;

    public int TypeId => typeId;
    public SpriteRenderer SpriteRenderer { get; private set; }

    private void Awake()
    {
        SpriteRenderer = GetComponent<SpriteRenderer>();
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
        gameObject.name = "Sushi_Reset";
    }
}