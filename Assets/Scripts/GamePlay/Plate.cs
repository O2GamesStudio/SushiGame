using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Plate : MonoBehaviour
{
    private Stack<Sushi> activeStack = new Stack<Sushi>();
    private Queue<Layer> layerQueue = new Queue<Layer>();
    private PlateUI plateUI;

    public int ActiveCount => activeStack.Count;
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
        activeStack.Clear();
        layerQueue.Clear();

        foreach (var layer in layers)
        {
            layerQueue.Enqueue(layer);
        }

        foreach (var typeId in activeTypes)
        {
            var sushi = SushiPool.Instance.Get(typeId);
            activeStack.Push(sushi);
        }

        UpdateVisuals();
    }

    public void AddSushi(Sushi sushi)
    {
        if (IsFull) return;

        activeStack.Push(sushi);
        UpdateVisuals();
        CheckMerge();
    }

    public Sushi RemoveTop()
    {
        if (ActiveCount == 0) return null;

        var sushi = activeStack.Pop();
        UpdateVisuals();
        return sushi;
    }

    public Sushi PeekTop()
    {
        return ActiveCount > 0 ? activeStack.Peek() : null;
    }

    private void CheckMerge()
    {
        if (ActiveCount != 3) return;

        var sushis = new List<Sushi>(activeStack);
        if (sushis[0].TypeId == sushis[1].TypeId && sushis[1].TypeId == sushis[2].TypeId)
        {
            ExecuteMerge();
        }
    }

    private void ExecuteMerge()
    {
        while (activeStack.Count > 0)
        {
            var sushi = activeStack.Pop();
            SushiPool.Instance.Return(sushi);
        }

        if (layerQueue.Count > 0)
        {
            var nextLayer = layerQueue.Dequeue();
            var types = nextLayer.GetAllTypes();

            foreach (var typeId in types)
            {
                var sushi = SushiPool.Instance.Get(typeId);
                activeStack.Push(sushi);
            }
        }

        UpdateVisuals();
        
        if (IsEmpty)
        {
            GameStateChecker.Instance.CheckWinCondition();
        }
    }

    private void UpdateVisuals()
    {
        var sushis = new List<Sushi>(activeStack);
        
        for (int i = 0; i < sushis.Count; i++)
        {
            var sushi = sushis[sushis.Count - 1 - i];
            sushi.transform.position = transform.position + Vector3.up * i * 0.3f;
            sushi.transform.localScale = i == 0 ? Vector3.one : Vector3.one * 0.5f;
        }

        plateUI?.UpdateNextLayerDisplay(CurrentNextLayer);
    }
}
