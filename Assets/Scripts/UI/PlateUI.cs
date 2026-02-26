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
    [SerializeField] private Sprite[] lockIconSprites = new Sprite[3];
    [SerializeField] private Sprite hiddenSushiSprite;

    [Header("Reserve Plate Visuals")]
    [SerializeField] private Sprite reservePlateSprite;
    [SerializeField] private Sprite singleSlotReservePlateSprite;
    [SerializeField] private float reservePlateSpacing = 0.05f;
    [SerializeField] private Vector3 reservePlateStartOffset = new Vector3(0, -1.3f, 0);

    [Header("Lock Visuals")]
    [SerializeField] private SpriteRenderer plateSpriteRenderer;
    [SerializeField] private Sprite normalPlateSprite;
    [SerializeField] private Sprite lockedPlateSprite;
    [SerializeField] private Sprite singleSlotNormalPlateSprite;
    [SerializeField] private Sprite singleSlotLockedPlateSprite;
    [SerializeField] private GameObject requiredSushiIcon;
    [SerializeField] private SpriteRenderer requiredSushiRiceRenderer;
    [SerializeField] private SpriteRenderer requiredSushiToppingRenderer;
    [SerializeField] private GameObject adIcon;

    private List<GameObject> nextLayerIcons = new List<GameObject>();
    private List<SpriteRenderer> reservePlateRenderers = new List<SpriteRenderer>();
    private int slotCount = 3;

    private void OnDestroy()
    {
        ClearReservePlates();
    }

    public Sprite GetHiddenSushiSprite() => hiddenSushiSprite;

    public void SetSlotCount(int count)
    {
        slotCount = count;
        UpdatePlateSprite(PlateState.Normal);
    }

    private void UpdatePlateSprite(PlateState state)
    {
        if (plateSpriteRenderer == null) return;

        if (slotCount == 1)
            plateSpriteRenderer.sprite = state != PlateState.Normal ? singleSlotLockedPlateSprite : singleSlotNormalPlateSprite;
        else
            plateSpriteRenderer.sprite = state != PlateState.Normal ? lockedPlateSprite : normalPlateSprite;
    }

    public void UpdateLockState(PlateState state, int requiredSushiTypeId)
    {
        UpdatePlateSprite(state);

        if (state == PlateState.LockedSushi && requiredSushiTypeId >= 0)
        {
            if (requiredSushiIcon != null)
            {
                requiredSushiIcon.SetActive(true);

                var data = SushiPool.Instance.GetData(requiredSushiTypeId);
                if (data != null)
                {
                    if (requiredSushiRiceRenderer != null)
                        requiredSushiRiceRenderer.sprite = data.riceSprite;

                    if (requiredSushiToppingRenderer != null)
                    {
                        requiredSushiToppingRenderer.sprite = data.toppingSprite;
                        requiredSushiToppingRenderer.gameObject.SetActive(data.toppingSprite != null);
                        requiredSushiToppingRenderer.transform.localPosition = new Vector3(data.toppingOffsetX, data.toppingOffsetY, 0f);
                    }
                }
            }

            if (adIcon != null)
                adIcon.SetActive(false);
        }
        else if (state == PlateState.LockedAd)
        {
            if (adIcon != null)
                adIcon.SetActive(true);

            if (requiredSushiIcon != null)
                requiredSushiIcon.SetActive(false);

            ClearNextLayerDisplay();
        }
        else
        {
            if (requiredSushiIcon != null)
                requiredSushiIcon.SetActive(false);

            if (adIcon != null)
                adIcon.SetActive(false);
        }
    }

    public void UpdateNextLayerDisplay(Layer nextLayer)
    {
        ClearNextLayerDisplay();
        if (nextLayer == null) return;

        var types = nextLayer.GetAllTypes();
        var slotIndices = nextLayer.SlotIndices;
        var lockStages = nextLayer.GetLockStages();
        var hiddenStates = nextLayer.GetHiddenStates();

        for (int i = 0; i < types.Count; i++)
        {
            var icon = Instantiate(nextLayerIconPrefab, nextLayerContainer);

            float xPos = slotCount == 1 ? 0f : (slotIndices[i] - 1) * nextLayerIconSpacing;
            icon.transform.localPosition = new Vector3(xPos, nextLayerIconYOffset, 0f);
            icon.transform.localScale = Vector3.one * nextLayerIconScale;

            var sushiView = icon.GetComponent<Sushi>();
            var data = SushiPool.Instance.GetData(types[i]);

            if (sushiView != null && data != null)
                sushiView.Initialize(types[i], data.riceSprite, data.toppingSprite, data.sushiType, data.toppingOffsetX, data.toppingOffsetY);

            bool isHidden = hiddenStates != null && i < hiddenStates.Count && hiddenStates[i];
            if (isHidden && hiddenSushiSprite != null)
            {
                var hiddenObj = new GameObject("HiddenOverlay");
                hiddenObj.transform.SetParent(icon.transform);
                hiddenObj.transform.localPosition = Vector3.zero;
                hiddenObj.transform.localScale = Vector3.one * 1.5f;

                var hiddenRenderer = hiddenObj.AddComponent<SpriteRenderer>();
                hiddenRenderer.sprite = hiddenSushiSprite;

                if (sushiView?.SpriteRenderer != null)
                {
                    hiddenRenderer.sortingLayerName = sushiView.SpriteRenderer.sortingLayerName;
                    hiddenRenderer.sortingOrder = sushiView.SpriteRenderer.sortingOrder + 3;
                }
            }

            if (sushiView != null && lockStages[i] > 0)
                sushiView.SetLockStage(lockStages[i]);

            nextLayerIcons.Add(icon);
        }
    }

    private void ClearNextLayerDisplay()
    {
        foreach (var icon in nextLayerIcons)
        {
            if (icon != null)
                Destroy(icon);
        }
        nextLayerIcons.Clear();
    }

    public void UpdateReservePlates(int layerCount)
    {
        Sprite targetSprite = slotCount == 1 ? singleSlotReservePlateSprite : reservePlateSprite;
        if (targetSprite == null) return;

        while (reservePlateRenderers.Count < layerCount)
        {
            var plateObj = new GameObject($"ReservePlate_{reservePlateRenderers.Count}");
            plateObj.transform.SetParent(transform);

            var renderer = plateObj.AddComponent<SpriteRenderer>();
            renderer.sprite = targetSprite;
            renderer.sortingLayerName = "Plate";
            renderer.sortingOrder = -1 - reservePlateRenderers.Count;

            reservePlateRenderers.Add(renderer);
        }

        while (reservePlateRenderers.Count > layerCount)
        {
            int lastIndex = reservePlateRenderers.Count - 1;
            if (reservePlateRenderers[lastIndex] != null)
                Destroy(reservePlateRenderers[lastIndex].gameObject);
            reservePlateRenderers.RemoveAt(lastIndex);
        }

        for (int i = 0; i < reservePlateRenderers.Count; i++)
        {
            if (reservePlateRenderers[i] != null)
            {
                reservePlateRenderers[i].sprite = targetSprite;
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
                Destroy(renderer.gameObject);
        }
        reservePlateRenderers.Clear();
    }
}