using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelGenerator
{
    private LevelData levelData;
    private List<int> allSushiTypes;
    private HashSet<int> adPlateIndices;

    public LevelGenerator(LevelData data)
    {
        levelData = data;
        adPlateIndices = new HashSet<int>();
        GenerateSushiPool();
    }

    private void GenerateSushiPool()
    {
        allSushiTypes = new List<int>();

        int totalSushiCount = (levelData.totalSushiCount / 3) * 3;
        int sushisPerType = (totalSushiCount / levelData.sushiTypeCount / 3) * 3;
        int remainingSushis = totalSushiCount - (sushisPerType * levelData.sushiTypeCount);

        for (int typeId = 1; typeId <= levelData.sushiTypeCount; typeId++)
        {
            int count = sushisPerType;

            if (remainingSushis > 0)
            {
                int addCount = Mathf.Min(3, remainingSushis);
                count += addCount;
                remainingSushis -= addCount;
            }

            for (int i = 0; i < count; i++)
            {
                allSushiTypes.Add(typeId);
            }
        }

        Debug.Log($"[LevelGenerator] 총 초밥 개수: {allSushiTypes.Count} (3의 배수: {allSushiTypes.Count % 3 == 0})");

        Shuffle(allSushiTypes);
    }

    public List<PlateData> GeneratePlates()
    {
        DetermineAdPlates();

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

            bool isAdPlate = adPlateIndices.Contains(i);
            int layerCount = isAdPlate ? 0 : Random.Range(levelData.minLayersPerPlate, levelData.maxLayersPerPlate + 1);
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

        while (index < allSushiTypes.Count)
        {
            int lastPlateIndex = plates.Count - 1;
            if (lastPlateIndex >= 0 && !adPlateIndices.Contains(lastPlateIndex))
            {
                if (plates[lastPlateIndex].ActiveTypes.Count < 3)
                {
                    plates[lastPlateIndex].ActiveTypes.Add(allSushiTypes[index++]);
                }
                else
                {
                    var lastLayer = new List<int>();
                    while (index < allSushiTypes.Count && lastLayer.Count < 3)
                    {
                        lastLayer.Add(allSushiTypes[index++]);
                    }

                    if (lastLayer.Count > 0)
                    {
                        plates[lastPlateIndex].Layers.Add(new Layer(lastLayer));
                    }
                }
            }
            else
            {
                break;
            }
        }

        AssignLockedPlates(plates);

        ValidatePlates(plates);

        return plates;
    }

    private void DetermineAdPlates()
    {
        adPlateIndices.Clear();

        if (levelData.lockedPlateCount <= 0) return;

        int adUnlockCount = levelData.lockedPlateCount - levelData.mergeUnlockCount;
        if (adUnlockCount <= 0) return;

        var availablePlates = new List<int>();
        for (int i = 0; i < levelData.plateCount; i++)
        {
            availablePlates.Add(i);
        }
        Shuffle(availablePlates);

        for (int i = 0; i < Mathf.Min(adUnlockCount, availablePlates.Count); i++)
        {
            adPlateIndices.Add(availablePlates[i]);
        }
    }

    private void ValidatePlates(List<PlateData> plates)
    {
        int totalSushis = 0;
        foreach (var plate in plates)
        {
            totalSushis += plate.ActiveTypes.Count;
            foreach (var layer in plate.Layers)
            {
                totalSushis += layer.Count;
            }
        }

        Debug.Log($"[LevelGenerator] 배치된 총 초밥: {totalSushis}, 3의 배수: {totalSushis % 3 == 0}");

        if (totalSushis % 3 != 0)
        {
            Debug.LogError($"[LevelGenerator] 경고: 초밥이 3의 배수가 아닙니다! ({totalSushis}개)");
        }
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

        for (int i = 0; i < mergeUnlockCount; i++)
        {
            int plateIndex = platesToLock[i];
            int sushiType = availableSushiTypes[Random.Range(0, availableSushiTypes.Count)];

            plates[plateIndex].State = PlateState.LockedSushi;
            plates[plateIndex].RequiredSushiTypeId = sushiType;
        }

        int adStartIndex = mergeUnlockCount;
        foreach (var adIndex in adPlateIndices)
        {
            if (adStartIndex < lockedPlateCount)
            {
                plates[adIndex].State = PlateState.LockedAd;
                adStartIndex++;
            }
        }

        AssignLockedSushis(plates);
    }

    private void AssignLockedSushis(List<PlateData> plates)
    {
        if (levelData.lockedSushiCount <= 0) return;

        var availableSlots = new List<(int plateIndex, int slotIndex, bool isActive, int layerIndex)>();

        for (int i = 0; i < plates.Count; i++)
        {
            if (plates[i].State != PlateState.Normal) continue;

            for (int j = 0; j < plates[i].ActiveTypes.Count; j++)
            {
                availableSlots.Add((i, j, true, -1));
            }

            for (int layerIdx = 0; layerIdx < plates[i].Layers.Count; layerIdx++)
            {
                var layer = plates[i].Layers[layerIdx];
                for (int j = 0; j < layer.Count; j++)
                {
                    availableSlots.Add((i, j, false, layerIdx));
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
                plates[slot.plateIndex].Layers[slot.layerIndex].SetLockStage(slot.slotIndex, 3);
            }
        }
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