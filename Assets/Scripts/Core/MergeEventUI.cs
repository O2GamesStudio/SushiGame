using UnityEngine;
using System.Collections.Generic;

public class MergeEventUI : MonoBehaviour
{
    [SerializeField] private GameObject eventRoot;
    [SerializeField] private UnityEngine.UI.Image timerFillAmount;
    [SerializeField] private EventSushiIcon[] sushiSlots;

    [SerializeField] private Color normalEventColor = Color.white;
    [SerializeField] private Color specialPlateEventColor = Color.red;

    private List<int> slotTypeIds = new List<int>();

    private void Awake()
    {
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

            var bg = sushiSlots[i].GetComponent<UnityEngine.UI.Image>();
            if (bg != null)
                bg.color = i < specialMergeCount ? specialPlateEventColor : normalEventColor;

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
    }

    public void RemoveSushi(int typeId)
    {
        int index = slotTypeIds.IndexOf(typeId);
        if (index < 0 || index >= sushiSlots.Length) return;

        sushiSlots[index].gameObject.SetActive(false);
        slotTypeIds[index] = -1;
    }

    public void UpdateTimer(float remaining, float total)
    {
        if (timerFillAmount == null) return;
        timerFillAmount.fillAmount = total > 0f ? Mathf.Clamp01(remaining / total) : 0f;
    }

    public void HideEvent()
    {
        eventRoot?.SetActive(false);

        foreach (var slot in sushiSlots)
        {
            if (slot == null) continue;
            var bg = slot.GetComponent<UnityEngine.UI.Image>();
            if (bg != null)
                bg.color = normalEventColor;
            slot.gameObject.SetActive(false);
        }

        slotTypeIds.Clear();
    }
}