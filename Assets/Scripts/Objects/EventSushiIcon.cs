using UnityEngine;
using UnityEngine.UI;

public class EventSushiIcon : MonoBehaviour
{
    [SerializeField] private Image riceImage;
    [SerializeField] private Image toppingImage;

    public void SetData(SushiData data)
    {
        if (riceImage != null)
        {
            riceImage.sprite = data.riceSprite;
            riceImage.SetNativeSize();
        }

        if (toppingImage != null)
        {
            bool hasTopping = data.toppingSprite != null;
            toppingImage.gameObject.SetActive(hasTopping);

            if (hasTopping)
            {
                toppingImage.sprite = data.toppingSprite;
                toppingImage.SetNativeSize();
                toppingImage.rectTransform.anchoredPosition = new Vector2(
                    data.toppingOffsetX * data.riceSprite.pixelsPerUnit,
                    data.toppingOffsetY * data.riceSprite.pixelsPerUnit
                );
            }
        }
    }
}