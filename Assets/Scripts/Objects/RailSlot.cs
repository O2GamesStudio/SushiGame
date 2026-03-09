using UnityEngine;

public class RailSlot : MonoBehaviour
{
    [SerializeField] private SpriteRenderer plateRenderer;

    public Sushi OccupiedSushi { get; private set; }
    public bool IsEmpty => OccupiedSushi == null;
    public Vector3 WorldPosition => transform.position;

    private void Awake()
    {
        if (plateRenderer != null)
            plateRenderer.transform.localPosition = Vector3.zero;
    }

    public void SetPlateSprite(Sprite sprite)
    {
        if (plateRenderer != null)
        {
            plateRenderer.sprite = sprite;
            plateRenderer.transform.localPosition = Vector3.zero;
        }
    }

    public void PlaceSushi(Sushi sushi)
    {
        OccupiedSushi = sushi;
        sushi.transform.SetParent(transform);
        sushi.transform.localPosition = new Vector3(0f, sushi.PlateOffsetY, -1f);
        sushi.transform.localScale = Vector3.one;
    }

    public void ClearSushi()
    {
        OccupiedSushi = null;
    }

    public Sushi RemoveSushi()
    {
        var sushi = OccupiedSushi;
        OccupiedSushi = null;
        return sushi;
    }
}