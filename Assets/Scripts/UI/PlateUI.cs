using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlateUI : MonoBehaviour
{
    [SerializeField] private Transform nextLayerContainer;
    [SerializeField] private GameObject nextLayerIconPrefab;

    private readonly Vector3[] slotPositions = new Vector3[]
    {
        new Vector3(-0.6f, -1f, 0),
        new Vector3(0f, -1f, 0),
        new Vector3(0.6f, -1f, 0)
    };

    private List<GameObject> nextLayerIcons = new List<GameObject>();

    public void UpdateNextLayerDisplay(Layer nextLayer)
    {
        foreach (var icon in nextLayerIcons)
        {
            Destroy(icon);
        }
        nextLayerIcons.Clear();

        if (nextLayer == null) return;

        var types = nextLayer.GetAllTypes();
        var availableSlots = Enumerable.Range(0, 3).ToList();

        for (int i = 0; i < types.Count; i++)
        {
            int randomIndex = Random.Range(0, availableSlots.Count);
            int slotIndex = availableSlots[randomIndex];
            availableSlots.RemoveAt(randomIndex);

            var icon = Instantiate(nextLayerIconPrefab, nextLayerContainer);
            icon.transform.localPosition = slotPositions[slotIndex];
            icon.transform.localScale = Vector3.one * 0.5f;

            var spriteRenderer = icon.GetComponent<SpriteRenderer>();
            var data = SushiPool.Instance.GetData(types[i]);
            if (data != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = data.sprite;
            }

            nextLayerIcons.Add(icon);
        }
    }
}