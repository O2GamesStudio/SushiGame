using UnityEngine;
using System.Collections.Generic;

public class PlateUI : MonoBehaviour
{
    [SerializeField] private Transform nextLayerContainer;
    [SerializeField] private GameObject nextLayerIconPrefab;

    [Header("Next Layer Icons")]
    [SerializeField] private float nextLayerIconYOffset = -1.2f;
    [SerializeField] private float nextLayerIconSpacing = 0.6f;
    [SerializeField] private float nextLayerIconScale = 0.5f;

    [Header("Reserve Plate Visuals")]
    [SerializeField] private Sprite reservePlateSprite;
    [SerializeField] private float reservePlateSpacing = 0.05f;
    [SerializeField] private Vector3 reservePlateStartOffset = new Vector3(0, -1.3f, 0);

    [Header("Lock Visuals")]
    [SerializeField] private SpriteRenderer plateSpriteRenderer;
    [SerializeField] private Sprite normalPlateSprite;
    [SerializeField] private Sprite lockedPlateSprite;
    [SerializeField] private GameObject requiredSushiIcon;
    [SerializeField] private GameObject adIcon;

    private List<GameObject> nextLayerIcons = new List<GameObject>();
    private List<SpriteRenderer> reservePlateRenderers = new List<SpriteRenderer>();

    private void OnDestroy()
    {
        ClearReservePlates();
    }

    public void UpdateLockState(PlateState state, int requiredSushiTypeId)
    {
        bool isLocked = state != PlateState.Normal;

        if (plateSpriteRenderer != null)
        {
            plateSpriteRenderer.sprite = isLocked ? lockedPlateSprite : normalPlateSprite;
        }

        if (state == PlateState.LockedSushi && requiredSushiTypeId >= 0)
        {
            if (requiredSushiIcon != null)
            {
                requiredSushiIcon.SetActive(true);
                var renderer = requiredSushiIcon.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    var data = SushiPool.Instance.GetData(requiredSushiTypeId);
                    if (data != null)
                    {
                        renderer.sprite = data.sprite;
                    }
                }
            }

            if (adIcon != null)
            {
                adIcon.SetActive(false);
            }
        }
        else if (state == PlateState.LockedAd)
        {
            if (adIcon != null)
            {
                adIcon.SetActive(true);
            }

            if (requiredSushiIcon != null)
            {
                requiredSushiIcon.SetActive(false);
            }
        }
        else
        {
            if (requiredSushiIcon != null)
            {
                requiredSushiIcon.SetActive(false);
            }

            if (adIcon != null)
            {
                adIcon.SetActive(false);
            }
        }

        if (!isLocked)
        {
            ClearNextLayerDisplay();
        }
    }

    public void UpdateNextLayerDisplay(Layer nextLayer)
    {
        ClearNextLayerDisplay();

        if (nextLayer == null) return;

        var types = nextLayer.GetAllTypes();
        var slotIndices = nextLayer.SlotIndices;

        for (int i = 0; i < types.Count; i++)
        {
            var icon = Instantiate(nextLayerIconPrefab, nextLayerContainer);

            float xPos = (slotIndices[i] - 1) * nextLayerIconSpacing;
            icon.transform.localPosition = new Vector3(xPos, nextLayerIconYOffset, 0);
            icon.transform.localScale = Vector3.one * nextLayerIconScale;

            var spriteRenderer = icon.GetComponent<SpriteRenderer>();
            var data = SushiPool.Instance.GetData(types[i]);
            if (data != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = data.sprite;
            }

            nextLayerIcons.Add(icon);
        }
    }

    private void ClearNextLayerDisplay()
    {
        foreach (var icon in nextLayerIcons)
        {
            if (icon != null)
            {
                Destroy(icon);
            }
        }
        nextLayerIcons.Clear();
    }

    public void UpdateReservePlates(int layerCount)
    {
        if (reservePlateSprite == null) return;

        while (reservePlateRenderers.Count < layerCount)
        {
            var plateObj = new GameObject($"ReservePlate_{reservePlateRenderers.Count}");
            plateObj.transform.SetParent(transform);

            var renderer = plateObj.AddComponent<SpriteRenderer>();
            renderer.sprite = reservePlateSprite;
            renderer.sortingLayerName = "Plate";
            renderer.sortingOrder = -1 - reservePlateRenderers.Count;

            reservePlateRenderers.Add(renderer);
        }

        while (reservePlateRenderers.Count > layerCount)
        {
            int lastIndex = reservePlateRenderers.Count - 1;
            if (reservePlateRenderers[lastIndex] != null)
            {
                Destroy(reservePlateRenderers[lastIndex].gameObject);
            }
            reservePlateRenderers.RemoveAt(lastIndex);
        }

        for (int i = 0; i < reservePlateRenderers.Count; i++)
        {
            if (reservePlateRenderers[i] != null)
            {
                Vector3 position = transform.position + reservePlateStartOffset + Vector3.down * (i * reservePlateSpacing);
                reservePlateRenderers[i].transform.position = position;
            }
        }
    }

    private void ClearReservePlates()
    {
        foreach (var renderer in reservePlateRenderers)
        {
            if (renderer != null && renderer.gameObject != null)
            {
                Destroy(renderer.gameObject);
            }
        }
        reservePlateRenderers.Clear();
    }
}