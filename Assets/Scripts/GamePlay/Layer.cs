using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Layer
{
    public List<int> SushiTypes { get; private set; }
    public List<int> SlotIndices { get; private set; }
    public List<int> LockStages { get; private set; }
    public List<bool> HiddenStates { get; private set; }
    public int Count => SushiTypes.Count;

    public Layer(List<int> sushiTypes)
    {
        SushiTypes = sushiTypes;
        LockStages = new List<int>();
        HiddenStates = new List<bool>();

        if (Count == 3 && sushiTypes[0] == sushiTypes[1] && sushiTypes[1] == sushiTypes[2])
        {
            Debug.LogError("Layer validation failed: Same type 3 sushis in one layer!");
        }

        GenerateSlotIndices();
        InitializeLockStages();
        InitializeHiddenStates();
    }

    private void GenerateSlotIndices()
    {
        var availableSlots = Enumerable.Range(0, 3).ToList();
        SlotIndices = new List<int>();

        for (int i = 0; i < Count; i++)
        {
            if (availableSlots.Count == 0)
                availableSlots = Enumerable.Range(0, 3).ToList();

            int randomIndex = Random.Range(0, availableSlots.Count);
            int slotIndex = availableSlots[randomIndex];
            availableSlots.RemoveAt(randomIndex);
            SlotIndices.Add(slotIndex);
        }
    }
    public void AddSushi(int typeId)
    {
        SushiTypes.Add(typeId);
        LockStages.Add(0);
        HiddenStates.Add(false);

        // SlotIndices 업데이트
        var usedSlots = new HashSet<int>(SlotIndices);
        for (int s = 0; s < 3; s++)
        {
            if (!usedSlots.Contains(s))
            {
                SlotIndices.Add(s);
                return;
            }
        }
        // 모든 슬롯 사용 중이면 랜덤
        SlotIndices.Add(Random.Range(0, 3));
    }
    private void InitializeLockStages()
    {
        for (int i = 0; i < Count; i++)
        {
            LockStages.Add(0);
        }
    }

    private void InitializeHiddenStates()
    {
        for (int i = 0; i < Count; i++)
        {
            HiddenStates.Add(false);
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

    public void SetHiddenState(int index, bool hidden)
    {
        if (index >= 0 && index < HiddenStates.Count)
        {
            HiddenStates[index] = hidden;
        }
    }

    public bool GetHiddenState(int index)
    {
        if (index >= 0 && index < HiddenStates.Count)
        {
            return HiddenStates[index];
        }
        return false;
    }

    public List<bool> GetHiddenStates()
    {
        return new List<bool>(HiddenStates);
    }
}