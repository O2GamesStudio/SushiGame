using UnityEngine;

public class EventSushiIcon : MonoBehaviour
{
    [SerializeField] private SpriteRenderer riceRenderer;
    [SerializeField] private SpriteRenderer toppingRenderer;

    public void SetData(SushiData data)
    {
        if (riceRenderer != null)
            riceRenderer.sprite = data.riceSprite;

        if (toppingRenderer != null)
        {
            toppingRenderer.sprite = data.toppingSprite;
            toppingRenderer.transform.localPosition = new Vector3(data.toppingOffsetX, data.toppingOffsetY, 0f);
        }
    }
}