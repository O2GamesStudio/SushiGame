using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MergeEventUI : MonoBehaviour
{
    [SerializeField] private GameObject eventRoot;
    [SerializeField] private Image timerFillAmount;
    [SerializeField] private Transform eventSushiContainer;
    [SerializeField] private GameObject eventSushiIconPrefab;

    private List<GameObject> sushiIcons = new List<GameObject>();
    private Dictionary<int, GameObject> typeToIcon = new Dictionary<int, GameObject>();

    public void ShowEvent(List<int> sushiTypes)
    {
        ClearIcons();
        eventRoot?.SetActive(true);

        foreach (var typeId in sushiTypes)
        {
            var icon = Instantiate(eventSushiIconPrefab, eventSushiContainer);
            var data = SushiPool.Instance.GetData(typeId);

            if (data != null)
            {
                var iconView = icon.GetComponent<EventSushiIcon>();
                if (iconView != null)
                    iconView.SetData(data);
            }

            sushiIcons.Add(icon);
            typeToIcon[typeId] = icon;
        }
    }

    public void RemoveSushi(int typeId)
    {
        if (!typeToIcon.ContainsKey(typeId)) return;

        var icon = typeToIcon[typeId];
        typeToIcon.Remove(typeId);
        sushiIcons.Remove(icon);
        Destroy(icon);
    }

    public void UpdateTimer(float remaining, float total)
    {
        if (timerFillAmount == null) return;
        timerFillAmount.fillAmount = total > 0f ? Mathf.Clamp01(remaining / total) : 0f;
    }

    public void HideEvent()
    {
        eventRoot?.SetActive(false);
        ClearIcons();
    }

    private void ClearIcons()
    {
        foreach (var icon in sushiIcons)
        {
            if (icon != null)
                Destroy(icon);
        }
        sushiIcons.Clear();
        typeToIcon.Clear();
    }
}