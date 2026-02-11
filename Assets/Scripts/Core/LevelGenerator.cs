using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelGenerator
{
    private LevelData levelData;
    private List<int> allSushiTypes;
    private HashSet<int> adPlateIndices;
    private HashSet<int> sushiMergePlateIndices;
    private List<int> selectedSushiTypes;
    private List<List<int>> cachedGuaranteedSushis;

    public LevelGenerator(LevelData data)
    {
        levelData = data;
        adPlateIndices = new HashSet<int>();
        sushiMergePlateIndices = new HashSet<int>();
        SelectRandomSushiTypes();
        GenerateSushiPool();
    }

    private void SelectRandomSushiTypes()
    {
        var allTypes = SushiPool.Instance.GetAllAvailableTypeIds();

        if (allTypes.Count < levelData.sushiTypeCount)
        {
            Debug.LogError($"[LevelGenerator] 사용 가능한 스시 타입({allTypes.Count})이 필요한 개수({levelData.sushiTypeCount})보다 적습니다!");
            selectedSushiTypes = allTypes;
            return;
        }

        Shuffle(allTypes);
        selectedSushiTypes = allTypes.GetRange(0, levelData.sushiTypeCount);
    }

    private void GenerateSushiPool()
    {
        allSushiTypes = new List<int>();

        int totalSushiCount = (levelData.totalSushiCount / 3) * 3;
        int basePerType = (totalSushiCount / levelData.sushiTypeCount / 3) * 3;

        foreach (var typeId in selectedSushiTypes)
        {
            for (int i = 0; i < basePerType; i++)
            {
                allSushiTypes.Add(typeId);
            }
        }

        int remaining = totalSushiCount - allSushiTypes.Count;
        int typeIndex = 0;

        while (remaining > 0)
        {
            int addCount = Mathf.Min(3, remaining);
            for (int i = 0; i < addCount; i++)
            {
                allSushiTypes.Add(selectedSushiTypes[typeIndex]);
            }
            remaining -= addCount;
            typeIndex = (typeIndex + 1) % selectedSushiTypes.Count;
        }

        Debug.Log($"[LevelGenerator] 총 초밥 개수: {allSushiTypes.Count}");

        Shuffle(allSushiTypes);
    }

    public List<PlateData> GeneratePlates()
    {
        cachedGuaranteedSushis = ExtractGuaranteedSushis();
        DetermineLockedPlates();

        var plates = new List<PlateData>();
        int index = 0;

        for (int i = 0; i < levelData.plateCount; i++)
        {
            var plateData = new PlateData();
            bool isAdPlate = adPlateIndices.Contains(i);

            if (!isAdPlate)
            {
                if (i < cachedGuaranteedSushis.Count)
                {
                    plateData.ActiveTypes = cachedGuaranteedSushis[i];
                }
                else
                {
                    int activeCount = Random.Range(1, 4);
                    for (int j = 0; j < activeCount && index < allSushiTypes.Count; j++)
                    {
                        plateData.ActiveTypes.Add(allSushiTypes[index++]);
                    }

                    if (HasSameThree(plateData.ActiveTypes))
                    {
                        FixSameThree(plateData.ActiveTypes, ref index);
                    }
                }
            }

            int layerCount = isAdPlate ? 0 : Random.Range(levelData.minLayersPerPlate, levelData.maxLayersPerPlate + 1);

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

        DistributeRemainingSushis(plates, ref index);
        AssignLockedPlates(plates);
        ValidatePlates(plates);

        return plates;
    }

    private void DistributeRemainingSushis(List<PlateData> plates, ref int index)
    {
        if (index >= allSushiTypes.Count) return;

        var availablePlates = new List<int>();
        for (int i = 0; i < plates.Count; i++)
        {
            if (!adPlateIndices.Contains(i))
            {
                availablePlates.Add(i);
            }
        }

        int currentPlateIdx = 0;

        while (index < allSushiTypes.Count && availablePlates.Count > 0)
        {
            int plateIndex = availablePlates[currentPlateIdx % availablePlates.Count];
            var plate = plates[plateIndex];

            if (plate.ActiveTypes.Count < 3)
            {
                plate.ActiveTypes.Add(allSushiTypes[index++]);
            }
            else if (plate.Layers.Count < levelData.maxLayersPerPlate)
            {
                var newLayer = new List<int>();
                int layerSize = Mathf.Min(3, allSushiTypes.Count - index);

                for (int i = 0; i < layerSize; i++)
                {
                    newLayer.Add(allSushiTypes[index++]);
                }

                if (HasSameThree(newLayer))
                {
                    FixSameThree(newLayer, ref index);
                }

                plate.Layers.Add(new Layer(newLayer));
            }
            else
            {
                availablePlates.RemoveAt(currentPlateIdx % availablePlates.Count);
                if (currentPlateIdx > 0) currentPlateIdx--;
                continue;
            }

            currentPlateIdx++;
        }
    }

    private void DetermineLockedPlates()
    {
        adPlateIndices.Clear();
        sushiMergePlateIndices.Clear();

        if (levelData.lockedPlateCount <= 0) return;

        int guaranteedPlateCount = cachedGuaranteedSushis.Count;

        var availablePlates = new List<int>();
        for (int i = guaranteedPlateCount; i < levelData.plateCount; i++)
        {
            availablePlates.Add(i);
        }
        Shuffle(availablePlates);

        int mergeUnlockCount = Mathf.Min(levelData.mergeUnlockCount, levelData.lockedPlateCount);

        int totalLockedCount = Mathf.Min(levelData.lockedPlateCount, availablePlates.Count);

        for (int i = 0; i < mergeUnlockCount && i < totalLockedCount; i++)
        {
            sushiMergePlateIndices.Add(availablePlates[i]);
        }

        for (int i = mergeUnlockCount; i < totalLockedCount; i++)
        {
            adPlateIndices.Add(availablePlates[i]);
        }
    }

    private void ValidatePlates(List<PlateData> plates)
    {
        int totalSushis = 0;
        var typeCount = new Dictionary<int, int>();

        foreach (var plate in plates)
        {
            foreach (var typeId in plate.ActiveTypes)
            {
                totalSushis++;
                if (!typeCount.ContainsKey(typeId)) typeCount[typeId] = 0;
                typeCount[typeId]++;
            }

            foreach (var layer in plate.Layers)
            {
                foreach (var typeId in layer.SushiTypes)
                {
                    totalSushis++;
                    if (!typeCount.ContainsKey(typeId)) typeCount[typeId] = 0;
                    typeCount[typeId]++;
                }
            }
        }

        Debug.Log($"[LevelGenerator] 배치된 총 초밥: {totalSushis}, 3의 배수: {totalSushis % 3 == 0}");

        foreach (var kvp in typeCount)
        {
            Debug.Log($"[LevelGenerator] 타입 {kvp.Key}: {kvp.Value}개 (3의 배수: {kvp.Value % 3 == 0})");
            if (kvp.Value % 3 != 0)
            {
                Debug.LogError($"[LevelGenerator] 타입 {kvp.Key}이(가) 3의 배수가 아닙니다!");
            }
        }

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

        foreach (var plateIndex in sushiMergePlateIndices)
        {
            int sushiType = availableSushiTypes[Random.Range(0, availableSushiTypes.Count)];
            plates[plateIndex].State = PlateState.LockedSushi;
            plates[plateIndex].RequiredSushiTypeId = sushiType;
        }

        foreach (var plateIndex in adPlateIndices)
        {
            plates[plateIndex].State = PlateState.LockedAd;
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

    private List<List<int>> ExtractGuaranteedSushis()
    {
        var result = new List<List<int>>();
        if (levelData.guaranteedMergeSets <= 0) return result;

        var typeCount = new Dictionary<int, int>();
        foreach (var typeId in allSushiTypes)
        {
            if (!typeCount.ContainsKey(typeId))
                typeCount[typeId] = 0;
            typeCount[typeId]++;
        }

        var availableTypes = selectedSushiTypes.Where(t => typeCount.ContainsKey(t) && typeCount[t] >= 3).ToList();
        Shuffle(availableTypes);

        int setsToCreate = Mathf.Min(levelData.guaranteedMergeSets, availableTypes.Count);

        for (int set = 0; set < setsToCreate; set++)
        {
            int typeId = availableTypes[set];

            var typeIndices = new List<int>();
            for (int i = 0; i < allSushiTypes.Count; i++)
            {
                if (allSushiTypes[i] == typeId)
                {
                    typeIndices.Add(i);
                    if (typeIndices.Count == 3) break;
                }
            }

            foreach (var idx in typeIndices.OrderByDescending(i => i))
            {
                allSushiTypes.RemoveAt(idx);
            }

            var possibleDistributions = new List<List<int>>
        {
            new List<int> { 1, 2 },
            new List<int> { 2, 1 },
            new List<int> { 1, 1, 1 }
        };
            var distribution = possibleDistributions[Random.Range(0, possibleDistributions.Count)];

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
        Shuffle(result);

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

    public PlateData()
    {
        ActiveTypes = new List<int>();
        Layers = new List<Layer>();
    }
}