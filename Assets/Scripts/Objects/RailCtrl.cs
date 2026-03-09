using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class RailCtrl : MonoBehaviour
{
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float slotSpacing = 1.5f;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject railPatternPrefab;
    [SerializeField] private float patternSpacing = 1.5f;

    private List<RailSlot> slots = new List<RailSlot>();
    private List<Transform> patterns = new List<Transform>();
    private bool isMoving = false;
    private float rightBoundX;
    private float leftBoundX;
    private float totalWidth;
    private float totalPatternWidth;

    public IReadOnlyList<RailSlot> Slots => slots;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        float camHalfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        rightBoundX = camHalfWidth + 3f;
        leftBoundX = -camHalfWidth;
    }

    public void Initialize(List<int> sushiTypeIds, Sprite railPlateSprite, GameObject slotPrefab)
    {
        totalWidth = sushiTypeIds.Count * slotSpacing;

        for (int i = 0; i < sushiTypeIds.Count; i++)
        {
            var slotObj = Instantiate(slotPrefab, slotsContainer);
            slotObj.transform.localPosition = new Vector3(rightBoundX - i * slotSpacing, 0f, 0f);

            var slot = slotObj.GetComponent<RailSlot>();
            slot.SetPlateSprite(railPlateSprite);

            var sushi = SushiPool.Instance.Get(sushiTypeIds[i]);
            sushi.SetCurrentPlate(null);
            slot.PlaceSushi(sushi);

            slots.Add(slot);
        }

        SpawnPatterns();
        isMoving = true;
    }

    private void SpawnPatterns()
    {
        if (railPatternPrefab == null) return;

        float spacing = slotSpacing * 0.8f;
        float screenWidth = rightBoundX - leftBoundX;
        int count = Mathf.CeilToInt(screenWidth / spacing) + 2;
        totalPatternWidth = count * spacing;

        for (int i = 0; i < count; i++)
        {
            var patternObj = Instantiate(railPatternPrefab, transform);
            patternObj.transform.localPosition = new Vector3(rightBoundX - i * spacing, 0f, 0f);
            patterns.Add(patternObj.transform);
        }
    }

    private void Update()
    {
        if (!isMoving) return;

        float delta = moveSpeed * Time.deltaTime;
        float negativeDelta = -delta;

        int slotCount = slots.Count;
        for (int i = 0; i < slotCount; i++)
        {
            var t = slots[i].transform;
            var pos = t.localPosition;
            pos.x += negativeDelta;
            if (pos.x < -rightBoundX)
                pos.x += totalWidth;
            t.localPosition = pos;
        }

        int patternCount = patterns.Count;
        for (int i = 0; i < patternCount; i++)
        {
            var t = patterns[i];
            var pos = t.localPosition;
            pos.x += negativeDelta;
            if (pos.x < -rightBoundX)
                pos.x += totalPatternWidth;
            t.localPosition = pos;
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