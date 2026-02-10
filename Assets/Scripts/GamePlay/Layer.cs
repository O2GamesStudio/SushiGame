using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Layer
{
    public List<int> SushiTypes { get; private set; }
    public List<int> SlotIndices { get; private set; }
    public List<int> LockStages { get; private set; }
    public int Count => SushiTypes.Count;

    public Layer(List<int> sushiTypes)
    {
        SushiTypes = sushiTypes;
        LockStages = new List<int>();

        if (Count == 3 && sushiTypes[0] == sushiTypes[1] && sushiTypes[1] == sushiTypes[2])
        {
            Debug.LogError("Layer validation failed: Same type 3 sushis in one layer!");
        }

        GenerateSlotIndices();
        InitializeLockStages();
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

    private void InitializeLockStages()
    {
        for (int i = 0; i < Count; i++)
        {
            LockStages.Add(0);
        }
    }

    public List<int> GetAllTypes()
    {
        return new List<int>(SushiTypes);
    }

    public void SetLockStage(int index, int stage)
    {
        if (index >= 0 && index < LockStages.Count)
        {
            LockStages[index] = stage;
        }
    }

    public int GetLockStage(int index)
    {
        if (index >= 0 && index < LockStages.Count)
        {
            return LockStages[index];
        }
        return 0;
    }

    public List<int> GetLockStages()
    {
        return new List<int>(LockStages);
    }
}