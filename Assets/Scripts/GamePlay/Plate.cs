using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Plate : MonoBehaviour
{
    [SerializeField] private Transform[] sushiSlots = new Transform[3];

    private List<Sushi> activeSushis = new List<Sushi>(3) { null, null, null };
    private Queue<Layer> layerQueue = new Queue<Layer>();
    private PlateUI plateUI;

    public int ActiveCount => activeSushis.Count(s => s != null);
    public int LayerCount => layerQueue.Count;
    public bool IsFull => ActiveCount >= 3;
    public bool IsEmpty => ActiveCount == 0 && LayerCount == 0;
    public Layer CurrentNextLayer => layerQueue.Count > 0 ? layerQueue.Peek() : null;

    private void Awake()
    {
        plateUI = GetComponent<PlateUI>();
    }

    public void Initialize(List<int> activeTypes, List<Layer> layers)
    {
        activeSushis = new List<Sushi>(3) { null, null, null };
        layerQueue.Clear();

        foreach (var layer in layers)
        {
            layerQueue.Enqueue(layer);
        }

        int index = 0;
        foreach (var typeId in activeTypes)
        {
            var sushi = SushiPool.Instance.Get(typeId);
            activeSushis[index++] = sushi;
        }

        UpdateVisuals();
    }

    public void AddSushi(Sushi sushi, int preferredSlot = -1)
    {
        if (IsFull) return;

        if (preferredSlot >= 0 && preferredSlot < 3 && activeSushis[preferredSlot] == null)
        {
            activeSushis[preferredSlot] = sushi;
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                if (activeSushis[i] == null)
                {
                    activeSushis[i] = sushi;
                    break;
                }
            }
        }

        UpdateVisuals();
        CheckMerge();
    }

    public bool RemoveSpecificSushi(Sushi sushi)
    {
        for (int i = 0; i < 3; i++)
        {
            if (activeSushis[i] == sushi)
            {
                activeSushis[i] = null;
                UpdateVisuals();
                CheckNextLayerRefill();
                return true;
            }
        }
        return false;
    }

    public bool ContainsSushi(Sushi sushi)
    {
        return activeSushis.Contains(sushi);
    }

    public int GetClosestEmptySlot(Vector3 worldPosition)
    {
        int closestSlot = -1;
        float minDistance = float.MaxValue;

        for (int i = 0; i < 3; i++)
        {
            if (activeSushis[i] == null)
            {
                float distance = Vector3.Distance(sushiSlots[i].position, worldPosition);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestSlot = i;
                }
            }
        }

        return closestSlot;
    }

    private void CheckMerge()
    {
        if (ActiveCount != 3) return;

        var nonNullSushis = activeSushis.Where(s => s != null).ToList();

        if (nonNullSushis.Count == 3 &&
            nonNullSushis[0].TypeId == nonNullSushis[1].TypeId &&
            nonNullSushis[1].TypeId == nonNullSushis[2].TypeId)
        {
            ExecuteMerge();
        }
    }

    private void ExecuteMerge()
    {
        foreach (var sushi in activeSushis)
        {
            if (sushi != null)
            {
                SushiPool.Instance.Return(sushi);
            }
        }

        activeSushis = new List<Sushi>(3) { null, null, null };

        RefillFromNextLayer();

        UpdateVisuals();

        if (IsEmpty)
        {
            GameStateChecker.Instance.CheckWinCondition();
        }
    }

    private void CheckNextLayerRefill()
    {
        if (ActiveCount == 0 && LayerCount > 0)
        {
            RefillFromNextLayer();
            UpdateVisuals();
        }
    }

    private void RefillFromNextLayer()
    {
        if (layerQueue.Count > 0)
        {
            var nextLayer = layerQueue.Dequeue();
            var types = nextLayer.GetAllTypes();
            var slotIndices = nextLayer.SlotIndices;

            for (int i = 0; i < types.Count; i++)
            {
                var sushi = SushiPool.Instance.Get(types[i]);
                activeSushis[slotIndices[i]] = sushi;
            }
        }
    }

    private void UpdateVisuals()
    {
        for (int i = 0; i < 3; i++)
        {
            if (activeSushis[i] != null)
            {
                var sushi = activeSushis[i];
                sushi.transform.SetParent(sushiSlots[i]);
                sushi.transform.position = sushiSlots[i].position;
                sushi.transform.localPosition = new Vector3(0, 0, -1);
                sushi.transform.localScale = Vector3.one;
            }
        }

        plateUI?.UpdateNextLayerDisplay(CurrentNextLayer);
    }
}