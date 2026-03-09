using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class MergeEventUI : MonoBehaviour
{
    [SerializeField] private GameObject eventRoot;
    [SerializeField] private Image timerFillAmount;
    [SerializeField] private EventSushiIcon[] sushiSlots;

    [SerializeField] private Color normalEventColor = Color.white;
    [SerializeField] private Color specialPlateEventColor = Color.red;

    [Header("Animation")]
    [SerializeField] private float slideInOffsetX = -1400f;
    [SerializeField] private float slideOutOffsetX = 1400f;
    [SerializeField] private float slideInDuration = 0.4f;
    [SerializeField] private float slideOutDuration = 0.35f;
    [SerializeField] private Vector2 centerPosition;

    private List<int> slotTypeIds = new List<int>();
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = eventRoot?.GetComponent<RectTransform>();

        foreach (var slot in sushiSlots)
        {
            if (slot != null)
                slot.gameObject.SetActive(false);
        }
    }

    public void ShowEvent(List<int> sushiTypes, int specialMergeCount)
    {
        slotTypeIds.Clear();
        eventRoot?.SetActive(true);

        for (int i = 0; i < sushiSlots.Length; i++)
        {
            if (sushiSlots[i] == null) continue;

            sushiSlots[i].SetBgColor(i < specialMergeCount ? specialPlateEventColor : normalEventColor);

            if (i < sushiTypes.Count)
            {
                var data = SushiPool.Instance.GetData(sushiTypes[i]);
                if (data != null)
                    sushiSlots[i].SetData(data);

                sushiSlots[i].gameObject.SetActive(true);
                slotTypeIds.Add(sushiTypes[i]);
            }
            else
            {
                sushiSlots[i].gameObject.SetActive(false);
            }
        }

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = centerPosition + new Vector2(slideInOffsetX, 0f);
            rectTransform.DOAnchorPos(centerPosition, slideInDuration)
                .SetEase(Ease.OutCubic);
        }
    }

    public void RemoveSushi(int typeId)
    {
        int index = slotTypeIds.IndexOf(typeId);
        if (index < 0 || index >= sushiSlots.Length) return;

        sushiSlots[index].SetChecked();
        slotTypeIds[index] = -1;
    }

    public void UpdateTimer(float remaining, float total)
    {
        if (timerFillAmount == null) return;
        timerFillAmount.fillAmount = total > 0f ? Mathf.Clamp01(remaining / total) : 0f;
    }

    public void HideEvent()
    {
        if (rectTransform != null)
        {
            rectTransform.DOAnchorPos(centerPosition + new Vector2(slideOutOffsetX, 0f), slideOutDuration)
                .SetEase(Ease.InCubic)
                .OnComplete(() =>
                {
                    eventRoot?.SetActive(false);
                    rectTransform.anchoredPosition = centerPosition;
                    ResetSlots();
                });
        }
        else
        {
            eventRoot?.SetActive(false);
            ResetSlots();
        }
    }

    private void ResetSlots()
    {
        foreach (var slot in sushiSlots)
        {
            if (slot == null) continue;
            slot.SetBgColor(normalEventColor);
            slot.gameObject.SetActive(false);
        }
        slotTypeIds.Clear();
    }
}