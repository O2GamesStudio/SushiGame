using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Layer
{
    public List<int> SushiTypes { get; private set; }
    public List<int> SlotIndices { get; private set; }
    public int Count => SushiTypes.Count;

    public Layer(List<int> sushiTypes)
    {
        SushiTypes = sushiTypes;

        if (Count == 3 && sushiTypes[0] == sushiTypes[1] && sushiTypes[1] == sushiTypes[2])
        {
            Debug.LogError("Layer validation failed: Same type 3 sushis in one layer!");
        }

        GenerateSlotIndices();
    }

    private void GenerateSlotIndices()
    {
        var availableSlots = Enumerable.Range(0, 3).ToList();
        SlotIndices = new List<int>();

        for (int i = 0; i < Count; i++)
        {
            int randomIndex = Random.Range(0, availableSlots.Count);
            int slotIndex = availableSlots[randomIndex];
            availableSlots.RemoveAt(randomIndex);
            SlotIndices.Add(slotIndex);
        }
    }

    public List<int> GetAllTypes()
    {
        return new List<int>(SushiTypes);
    }
}