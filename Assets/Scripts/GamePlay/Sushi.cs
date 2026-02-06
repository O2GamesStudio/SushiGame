using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class Sushi : MonoBehaviour
{
    public int TypeId { get; private set; }
    public SpriteRenderer SpriteRenderer { get; private set; }

    private void Awake()
    {
        SpriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(int typeId, Sprite sprite)
    {
        TypeId = typeId;
        SpriteRenderer.sprite = sprite;
    }

    public void Reset()
    {
        TypeId = -1;
        transform.position = Vector3.zero;
        transform.localScale = Vector3.one;
    }
}
