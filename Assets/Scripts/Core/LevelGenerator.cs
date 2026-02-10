using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelGenerator
{
    private LevelData levelData;
    private List<int> allSushiTypes;

    public LevelGenerator(LevelData data)
    {
        levelData = data;
        GenerateSushiPool();
    }

    private void GenerateSushiPool()
    {
        allSushiTypes = new List<int>();

        int sushisPerType = (levelData.totalSushiCount / levelData.sushiTypeCount / 3) * 3;
        int remainingSushis = levelData.totalSushiCount - (sushisPerType * levelData.sushiTypeCount);
        remainingSushis = (remainingSushis / 3) * 3;

        for (int typeId = 1; typeId <= levelData.sushiTypeCount; typeId++)
        {
            int count = sushisPerType;

            if (typeId == 1 && remainingSushis > 0)
            {
                count += remainingSushis;
            }

            for (int i = 0; i < count; i++)
            {
                allSushiTypes.Add(typeId);
            }
        }

        Shuffle(allSushiTypes);
    }

    public List<PlateData> GeneratePlates()
    {
        var plates = new List<PlateData>();

        var guaranteedSushis = GenerateGuaranteedSushis();

        int index = 0;
        for (int i = 0; i < guaranteedSushis.Count; i++)
        {
            index += guaranteedSushis[i].Count;
        }

        for (int i = 0; i < levelData.plateCount; i++)
        {
            var plateData = new PlateData();

            if (i < guaranteedSushis.Count)
            {
                plateData.ActiveTypes = guaranteedSushis[i];
            }
            else
            {
                int activeCount = Random.Range(1, 4);
                plateData.ActiveTypes = new List<int>();
                for (int j = 0; j < activeCount && index < allSushiTypes.Count; j++)
                {
                    plateData.ActiveTypes.Add(allSushiTypes[index++]);
                }

                if (HasSameThree(plateData.ActiveTypes))
                {
                    FixSameThree(plateData.ActiveTypes, ref index);
                }
            }

            int layerCount = Random.Range(levelData.minLayersPerPlate, levelData.maxLayersPerPlate + 1);
            plateData.Layers = new List<Layer>();

            for (int j = 0; j < layerCount && index < allSushiTypes.Count; j++)
            {
                int layerSize = Random.Range(1, 4);
                var layerTypes = new List<int>();

                for (int k = 0; k < layerSize && index < allSushiTypes.Count; k++)
                {
                    layerTypes.Add(allSushiTypes[index++]);
                }

                if (HasSameThree(layerTypes))
                {
                    FixSameThree(layerTypes, ref index);
                }

                plateData.Layers.Add(new Layer(layerTypes));
            }

            plates.Add(plateData);
        }

        AssignLockedPlates(plates);

        return plates;
    }

    private void AssignLockedPlates(List<PlateData> plates)
    {
        if (levelData.lockedPlateCount <= 0 && levelData.lockedSushiCount <= 0) return;

        var availableSushiTypes = GetAvailableSushiTypes(plates);

        if (availableSushiTypes.Count == 0) return;

        var platesToLock = new List<int>();
        for (int i = 0; i < plates.Count; i++)
        {
            platesToLock.Add(i);
        }
        Shuffle(platesToLock);

        int lockedPlateCount = Mathf.Min(levelData.lockedPlateCount, platesToLock.Count);
        int mergeUnlockCount = Mathf.Min(levelData.mergeUnlockCount, lockedPlateCount);
        int adUnlockCount = lockedPlateCount - mergeUnlockCount;

        for (int i = 0; i < mergeUnlockCount; i++)
        {
            int plateIndex = platesToLock[i];
            int sushiType = availableSushiTypes[Random.Range(0, availableSushiTypes.Count)];

            plates[plateIndex].State = PlateState.LockedSushi;
            plates[plateIndex].RequiredSushiTypeId = sushiType;
        }

        for (int i = mergeUnlockCount; i < lockedPlateCount; i++)
        {
            int plateIndex = platesToLock[i];
            plates[plateIndex].State = PlateState.LockedAd;
        }

        AssignLockedSushis(plates);
    }

    private List<int> GetAvailableSushiTypes(List<PlateData> plates)
    {
        var sushiTypes = new HashSet<int>();

        foreach (var plate in plates)
        {
            foreach (var typeId in plate.ActiveTypes)
            {
                sushiTypes.Add(typeId);
            }

            foreach (var layer in plate.Layers)
            {
                foreach (var typeId in layer.SushiTypes)
                {
                    sushiTypes.Add(typeId);
                }
            }
        }

        return sushiTypes.ToList();
    }
    private void AssignLockedSushis(List<PlateData> plates)
    {
        if (levelData.lockedSushiCount <= 0) return;

        var availableSlots = new List<(int plateIndex, int slotIndex, bool isActive)>();

        for (int i = 0; i < plates.Count; i++)
        {
            if (plates[i].State != PlateState.Normal) continue;

            for (int j = 0; j < plates[i].ActiveTypes.Count; j++)
            {
                availableSlots.Add((i, j, true));
            }

            for (int layerIdx = 0; layerIdx < plates[i].Layers.Count; layerIdx++)
            {
                var layer = plates[i].Layers[layerIdx];
                var types = layer.GetAllTypes();
                for (int j = 0; j < types.Count; j++)
                {
                    availableSlots.Add((i, j, false));
                }
            }
        }

        Shuffle(availableSlots);

        int lockedCount = Mathf.Min(levelData.lockedSushiCount, availableSlots.Count);

        for (int i = 0; i < lockedCount; i++)
        {
            var slot = availableSlots[i];

            if (slot.isActive)
            {
                plates[slot.plateIndex].ActiveLockStages[slot.slotIndex] = 3;
            }
            else
            {
                int currentLayerIndex = 0;
                foreach (var layer in plates[slot.plateIndex].Layers)
                {
                    if (slot.slotIndex < layer.GetAllTypes().Count)
                    {
                        layer.SetLockStage(slot.slotIndex, 3);
                        break;
                    }
                    currentLayerIndex++;
                }
            }
        }
    }

    private List<List<int>> GenerateGuaranteedSushis()
    {
        var result = new List<List<int>>();
        var usedTypes = new HashSet<int>();

        for (int set = 0; set < levelData.guaranteedMergeSets; set++)
        {
            int typeId = -1;
            for (int i = 1; i <= levelData.sushiTypeCount; i++)
            {
                if (!usedTypes.Contains(i))
                {
                    typeId = i;
                    usedTypes.Add(i);
                    break;
                }
            }

            if (typeId == -1) break;

            var distribution = new List<int>();
            int remaining = 3;

            while (remaining > 0)
            {
                int count = Random.Range(1, Mathf.Min(3, remaining + 1));
                distribution.Add(count);
                remaining -= count;
            }

            foreach (var count in distribution)
            {
                var plateTypes = new List<int>();
                for (int i = 0; i < count; i++)
                {
                    plateTypes.Add(typeId);
                }
                result.Add(plateTypes);
            }
        }

        for (int i = result.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }

        return result;
    }

    private bool HasSameThree(List<int> types)
    {
        if (types.Count != 3) return false;
        return types[0] == types[1] && types[1] == types[2];
    }

    private void FixSameThree(List<int> types, ref int currentIndex)
    {
        if (currentIndex >= allSushiTypes.Count) return;

        int swapTarget = types[2];

        for (int i = currentIndex; i < allSushiTypes.Count; i++)
        {
            if (allSushiTypes[i] != types[0])
            {
                types[2] = allSushiTypes[i];
                allSushiTypes[i] = swapTarget;
                return;
            }
        }

        for (int i = 0; i < currentIndex; i++)
        {
            if (allSushiTypes[i] != types[0])
            {
                types[2] = allSushiTypes[i];
                allSushiTypes[i] = swapTarget;
                return;
            }
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

public class PlateData
{
    public List<int> ActiveTypes;
    public List<Layer> Layers;
    public PlateState State = PlateState.Normal;
    public int RequiredSushiTypeId = -1;
    public List<int> ActiveLockStages = new List<int> { 0, 0, 0 };
}