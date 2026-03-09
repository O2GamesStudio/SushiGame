using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class RailCtrl : MonoBehaviour
{
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float slotSpacing = 1.5f;
    [SerializeField] private Camera mainCamera;

    private List<RailSlot> slots = new List<RailSlot>();
    private bool isMoving = false;
    private float rightBoundX;
    private float leftBoundX;
    private float totalWidth;

    public IReadOnlyList<RailSlot> Slots => slots;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        float camHalfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        rightBoundX = camHalfWidth + 0.2f;
        leftBoundX = -camHalfWidth;
    }

    public void Initialize(List<int> sushiTypeIds, Sprite railPlateSprite, GameObject slotPrefab)
    {
        totalWidth = sushiTypeIds.Count * slotSpacing;

        for (int i = 0; i < sushiTypeIds.Count; i++)
        {
            var slotObj = Instantiate(slotPrefab, slotsContainer);
            slotObj.transform.localPosition = new Vector3(leftBoundX + i * slotSpacing, 0f, 0f);

            var slot = slotObj.GetComponent<RailSlot>();
            slot.SetPlateSprite(railPlateSprite);

            var sushi = SushiPool.Instance.Get(sushiTypeIds[i]);
            sushi.SetCurrentPlate(null);
            slot.PlaceSushi(sushi);

            slots.Add(slot);
        }

        isMoving = true;
    }

    private void Update()
    {
        if (!isMoving) return;

        float delta = moveSpeed * Time.deltaTime;
        foreach (var slot in slots)
        {
            slot.transform.localPosition += Vector3.right * delta;

            if (slot.transform.localPosition.x > rightBoundX)
                slot.transform.localPosition -= new Vector3(totalWidth, 0f, 0f);
        }
    }

    public void ReturnSushiToSlot(Sushi sushi, RailSlot slot)
    {
        sushi.transform.DOMove(slot.WorldPosition, 0.2f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                slot.PlaceSushi(sushi);
                sushi.SetDragState(false);
            });
    }

    public RailSlot GetSlotContainingSushi(Sushi sushi)
    {
        foreach (var slot in slots)
        {
            if (slot.OccupiedSushi == sushi)
                return slot;
        }
        return null;
    }

    public void SetMoving(bool moving)
    {
        isMoving = moving;
    }
}