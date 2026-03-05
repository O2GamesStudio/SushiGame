using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;

public class PlateUI : MonoBehaviour
{
    [SerializeField] private Transform nextLayerContainer;
    [SerializeField] private GameObject nextLayerIconPrefab;
    [SerializeField] private Sprite specialPlateSprite;

    [Header("Next Layer Icons")]
    [SerializeField] private float nextLayerIconYOffset = -1.2f;
    [SerializeField] private float nextLayerIconSpacing = 0.6f;
    [SerializeField] private float nextLayerIconScale = 0.5f;
    [SerializeField] private Sprite lockIconSprites;
    [SerializeField] private Sprite hiddenSushiSprite;

    [Header("Reserve Plate Visuals")]
    [SerializeField] private Sprite reservePlateSprite;
    [SerializeField] private Sprite singleSlotReservePlateSprite;
    [SerializeField] private float reservePlateSpacing = 0.05f;
    [SerializeField] private Vector3 reservePlateStartOffset = new Vector3(0, -1.3f, 0);

    [Header("Lock Visuals")]
    [SerializeField] private SpriteRenderer plateSpriteRenderer;
    [SerializeField] private Sprite normalPlateSprite;
    [SerializeField] private Sprite singleSlotNormalPlateSprite;
    [SerializeField] private GameObject lockLid;
    [SerializeField] private SpriteRenderer lockLidRenderer;
    [SerializeField] private SpriteRenderer requiredSushiRiceRenderer;
    [SerializeField] private SpriteRenderer requiredSushiToppingRenderer;
    [SerializeField] private Collider2D sushiResetCollider;
    [SerializeField] private GameObject adIcon;

    [SerializeField] private GameObject reservePlatePrefab;
    private Vector3 lockLidOriginalLocalPosition;
    private List<GameObject> nextLayerIcons = new List<GameObject>();
    private List<SpriteRenderer> reservePlateRenderers = new List<SpriteRenderer>();
    private int slotCount = 3;

    private void Awake()
    {
        if (lockLid != null)
            lockLidOriginalLocalPosition = lockLid.transform.localPosition;
    }
    private void OnDestroy()
    {
        ClearReservePlates();
    }

    public Sprite GetHiddenSushiSprite() => hiddenSushiSprite;

    public void SetSlotCount(int count)
    {
        slotCount = count;
        UpdatePlateSprite();
    }

    public void SetSpecialPlate(bool isSpecial)
    {
        if (plateSpriteRenderer == null) return;
        plateSpriteRenderer.sprite = isSpecial ? specialPlateSprite ?? normalPlateSprite : normalPlateSprite;
    }

    private void UpdatePlateSprite()
    {
        if (plateSpriteRenderer == null) return;
        plateSpriteRenderer.sprite = slotCount == 1 ? singleSlotNormalPlateSprite : normalPlateSprite;
    }

    public void UpdateLockState(PlateState state, int requiredSushiTypeId)
    {
        if (state == PlateState.LockedSushi && requiredSushiTypeId >= 0)
        {
            if (lockLid != null)
            {
                lockLidOriginalLocalPosition = lockLid.transform.localPosition;
                lockLid.SetActive(true);
            }
            if (sushiResetCollider != null) sushiResetCollider.enabled = false;
            if (adIcon != null) adIcon.SetActive(false);

            var data = SushiPool.Instance.GetData(requiredSushiTypeId);
            if (data != null)
            {
                if (requiredSushiRiceRenderer != null)
                {
                    requiredSushiRiceRenderer.sprite = data.riceSprite;
                    requiredSushiRiceRenderer.gameObject.SetActive(true);
                    requiredSushiRiceRenderer.transform.localPosition = new Vector3(1.1f, data.plateOffsetY + 0.15f, 0f);
                    requiredSushiRiceRenderer.transform.localScale = Vector3.one * 0.8f;

                    var col = requiredSushiRiceRenderer.GetComponent<Collider2D>();
                    if (col != null) col.enabled = false;
                }

                if (requiredSushiToppingRenderer != null)
                {
                    requiredSushiToppingRenderer.sprite = data.toppingSprite;
                    requiredSushiToppingRenderer.gameObject.SetActive(data.toppingSprite != null);
                    requiredSushiToppingRenderer.transform.localPosition = new Vector3(1.1f + data.toppingOffsetX, data.toppingOffsetY + data.plateOffsetY + 0.15f, 0f);
                    requiredSushiToppingRenderer.transform.localScale = Vector3.one * 0.8f;

                    var col = requiredSushiToppingRenderer.GetComponent<Collider2D>();
                    if (col != null) col.enabled = false;
                }
            }
        }
        else if (state == PlateState.LockedAd)
        {
            if (lockLid != null)
            {
                lockLidOriginalLocalPosition = lockLid.transform.localPosition;
                lockLid.SetActive(true);
            }
            if (sushiResetCollider != null) sushiResetCollider.enabled = false;
            if (adIcon != null) adIcon.SetActive(true);
            if (requiredSushiRiceRenderer != null) requiredSushiRiceRenderer.gameObject.SetActive(false);
            if (requiredSushiToppingRenderer != null) requiredSushiToppingRenderer.gameObject.SetActive(false);
        }
        else
        {
            if (lockLid != null) lockLid.SetActive(false);
            if (sushiResetCollider != null) sushiResetCollider.enabled = true;
            if (adIcon != null) adIcon.SetActive(false);
            if (requiredSushiRiceRenderer != null) requiredSushiRiceRenderer.gameObject.SetActive(false);
            if (requiredSushiToppingRenderer != null) requiredSushiToppingRenderer.gameObject.SetActive(false);
        }
    }

    public void PlayUnlockAnimation(System.Action onComplete = null)
    {
        HideLockLid(onComplete);
    }

    private void HideLockLid(System.Action onComplete = null)
    {
        if (lockLid == null || !lockLid.activeSelf)
        {
            onComplete?.Invoke();
            return;
        }

        var seq = DOTween.Sequence();
        seq.Append(lockLid.transform.DOLocalMoveY(lockLid.transform.localPosition.y + 1.5f, 0.5f)
            .SetEase(Ease.OutCubic));
        seq.OnComplete(() =>
        {
            lockLid.SetActive(false);
            lockLid.transform.localScale = Vector3.one;
            lockLid.transform.localPosition = new Vector3(
                lockLid.transform.localPosition.x, 0f, lockLid.transform.localPosition.z);
            if (sushiResetCollider != null) sushiResetCollider.enabled = true;
            if (requiredSushiRiceRenderer != null) requiredSushiRiceRenderer.gameObject.SetActive(false);
            if (requiredSushiToppingRenderer != null) requiredSushiToppingRenderer.gameObject.SetActive(false);
            if (adIcon != null) adIcon.SetActive(false);
            onComplete?.Invoke();
        });
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
            icon.transform.localScale = Vector3.one * nextLayerIconScale;

            var sushiView = icon.GetComponent<Sushi>();
            var data = SushiPool.Instance.GetData(types[i]);

            float offsetY = 0f;
            if (data != null)
                offsetY = data.sushiType == SushiType.Integrated ? data.plateOffsetY * 0.5f : data.plateOffsetY + 0.05f;

            float xPos = slotCount == 1 ? 0f : (slotIndices[i] - 1) * nextLayerIconSpacing;
            icon.transform.localPosition = new Vector3(xPos, nextLayerIconYOffset + offsetY, 0f);

            if (sushiView != null && data != null)
                sushiView.Initialize(types[i], data.riceSprite, data.toppingSprite, data.sushiType, data.toppingOffsetX, data.toppingOffsetY);

            bool isLocked = lockStages[i] > 0;
            bool isHidden = hiddenStates != null && i < hiddenStates.Count && hiddenStates[i];

            if (isHidden && !isLocked)
                sushiView?.SetHidden(true, hiddenSushiSprite);

            if (sushiView != null && isLocked && lockIconSprites != null)
                sushiView.ShowLockIcon(lockIconSprites);

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
        if (targetSprite == null || reservePlatePrefab == null) return;

        while (reservePlateRenderers.Count < layerCount)
        {
            var plateObj = LeanPool.Spawn(reservePlatePrefab, transform);
            var renderer = plateObj.GetComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Plate";
            renderer.sortingOrder = -1 - reservePlateRenderers.Count;
            reservePlateRenderers.Add(renderer);
        }

        while (reservePlateRenderers.Count > layerCount)
        {
            int lastIndex = reservePlateRenderers.Count - 1;
            if (reservePlateRenderers[lastIndex] != null)
                LeanPool.Despawn(reservePlateRenderers[lastIndex].gameObject);
            reservePlateRenderers.RemoveAt(lastIndex);
        }

        for (int i = 0; i < reservePlateRenderers.Count; i++)
        {
            if (reservePlateRenderers[i] == null) continue;
            reservePlateRenderers[i].sprite = targetSprite;
            reservePlateRenderers[i].transform.position =
                transform.position + reservePlateStartOffset + Vector3.down * (i * reservePlateSpacing);
        }
    }

    private void ClearReservePlates()
    {
        foreach (var renderer in reservePlateRenderers)
        {
            if (renderer != null && renderer.gameObject != null)
                LeanPool.Despawn(renderer.gameObject);
        }
        reservePlateRenderers.Clear();
    }
}