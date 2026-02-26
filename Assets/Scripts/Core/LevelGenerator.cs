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
    private HashSet<int> concentratedTypes;
    private HashSet<int> singleSlotPlateIndices;

    public LevelGenerator(LevelData data)
    {
        levelData = data;
        adPlateIndices = new HashSet<int>();
        sushiMergePlateIndices = new HashSet<int>();
        singleSlotPlateIndices = new HashSet<int>();
        concentratedTypes = new HashSet<int>();
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

        SelectConcentratedTypes();
    }

    private void SelectConcentratedTypes()
    {
        if (levelData.concentratedTypeCount <= 0) return;

        int count = Mathf.Min(levelData.concentratedTypeCount, selectedSushiTypes.Count);
        var shuffled = new List<int>(selectedSushiTypes);
        Shuffle(shuffled);

        for (int i = 0; i < count; i++)
        {
            concentratedTypes.Add(shuffled[i]);
        }

        Debug.Log($"[LevelGenerator] 집중 배치 타입: {string.Join(", ", concentratedTypes)}");
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

        if (concentratedTypes.Count > 0)
            GeneratePlatesWithConcentration(plates);
        else
            GeneratePlatesNormal(plates);

        CreateInitialEmptySlots(plates);
        AssignLockedPlates(plates);

        if (levelData.hiddenReserveCount > 0)
            ApplyHiddenReserves(plates);

        FixTypeMultiples(plates);
        ValidatePlates(plates);

        return plates;
    }
    private void FixTypeMultiples(List<PlateData> plates)
    {
        var typeCountMap = new Dictionary<int, int>();

        for (int i = 0; i < plates.Count; i++)
        {
            foreach (var typeId in plates[i].ActiveTypes)
            {
                if (!typeCountMap.ContainsKey(typeId)) typeCountMap[typeId] = 0;
                typeCountMap[typeId]++;
            }
            foreach (var layer in plates[i].Layers)
                foreach (var typeId in layer.SushiTypes)
                {
                    if (!typeCountMap.ContainsKey(typeId)) typeCountMap[typeId] = 0;
                    typeCountMap[typeId]++;
                }
        }

        foreach (var kvp in typeCountMap)
        {
            int typeId = kvp.Key;
            int count = kvp.Value;
            int remainder = count % 3;
            if (remainder == 0) continue;

            int toAdd = 3 - remainder;

            for (int added = 0; added < toAdd; added++)
            {
                bool placed = false;

                for (int i = 0; i < plates.Count && !placed; i++)
                {
                    if (adPlateIndices.Contains(i)) continue;
                    if (singleSlotPlateIndices.Contains(i)) continue;

                    if (plates[i].ActiveTypes.Count < 3)
                    {
                        plates[i].ActiveTypes.Add(typeId);
                        placed = true;
                    }
                }

                if (!placed)
                {
                    for (int i = 0; i < plates.Count && !placed; i++)
                    {
                        if (adPlateIndices.Contains(i)) continue;

                        if (plates[i].Layers.Count < levelData.maxLayersPerPlate)
                        {
                            plates[i].Layers.Add(new Layer(new List<int> { typeId }));
                            placed = true;
                        }
                    }
                }
            }
        }
    }

    private void ApplyHiddenReserves(List<PlateData> plates)
    {
        var allReserveIndices = new List<(PlateData plate, int layerIndex, int sushiIndex)>();

        foreach (var plate in plates)
        {
            if (plate.State == PlateState.LockedAd) continue;

            for (int layerIdx = 0; layerIdx < plate.Layers.Count; layerIdx++)
            {
                var layer = plate.Layers[layerIdx];
                for (int sushiIdx = 0; sushiIdx < layer.SushiTypes.Count; sushiIdx++)
                {
                    allReserveIndices.Add((plate, layerIdx, sushiIdx));
                }
            }
        }

        if (allReserveIndices.Count == 0)
        {
            Debug.LogWarning("[LevelGenerator] Reserve 초밥이 없어서 Hidden Reserve를 적용할 수 없습니다.");
            return;
        }

        Shuffle(allReserveIndices);

        int hiddenCount = Mathf.Min(levelData.hiddenReserveCount, allReserveIndices.Count);

        for (int i = 0; i < hiddenCount; i++)
        {
            var (plate, layerIndex, sushiIndex) = allReserveIndices[i];
            plate.Layers[layerIndex].SetHiddenState(sushiIndex, true);
        }

        Debug.Log($"[LevelGenerator] Hidden Reserve 적용: {hiddenCount}개");
    }

    private void GeneratePlatesWithConcentration(List<PlateData> plates)
    {
        var concentratedSushis = new Dictionary<int, List<int>>();
        var dispersedSushis = new List<int>();

        foreach (var typeId in allSushiTypes)
        {
            if (concentratedTypes.Contains(typeId))
            {
                if (!concentratedSushis.ContainsKey(typeId))
                    concentratedSushis[typeId] = new List<int>();
                concentratedSushis[typeId].Add(typeId);
            }
            else
            {
                dispersedSushis.Add(typeId);
            }
        }

        for (int i = 0; i < levelData.plateCount; i++)
            plates.Add(new PlateData());

        int currentPlateIndex = 0;
        var activeTypeCount = new Dictionary<int, int>();

        foreach (var plateTypes in cachedGuaranteedSushis)
        {
            plates[currentPlateIndex].ActiveTypes = new List<int>(plateTypes);

            foreach (var typeId in plateTypes)
            {
                if (!activeTypeCount.ContainsKey(typeId))
                    activeTypeCount[typeId] = 0;
                activeTypeCount[typeId]++;
            }

            currentPlateIndex++;
        }

        var concentratedTypesShuffled = concentratedTypes.ToList();
        Shuffle(concentratedTypesShuffled);

        var pendingLayerSushis = new List<int>();

        foreach (var typeId in concentratedTypesShuffled)
        {
            if (!concentratedSushis.ContainsKey(typeId)) continue;

            var typeSushis = concentratedSushis[typeId];
            if (typeSushis.Count == 0) continue;

            if (!activeTypeCount.ContainsKey(typeId))
                activeTypeCount[typeId] = 0;

            int targetPlateStart = currentPlateIndex % levelData.plateCount;
            int plateSpread = Random.Range(1, 3);

            for (int i = 0; i < typeSushis.Count; i++)
            {
                int targetPlate = (targetPlateStart + (i / 2)) % levelData.plateCount;
                int attempts = 0;

                while (adPlateIndices.Contains(targetPlate) && attempts < levelData.plateCount)
                {
                    targetPlate = (targetPlate + 1) % levelData.plateCount;
                    attempts++;
                }

                if (attempts >= levelData.plateCount)
                {
                    pendingLayerSushis.Add(typeSushis[i]);
                    continue;
                }

                var plate = plates[targetPlate];
                bool isSingleSlot = singleSlotPlateIndices.Contains(targetPlate);
                int maxActive = isSingleSlot ? 1 : 3;
                bool canAddToActive = plate.ActiveTypes.Count < maxActive && activeTypeCount[typeId] < maxActive;

                if (canAddToActive)
                {
                    plate.ActiveTypes.Add(typeSushis[i]);
                    activeTypeCount[typeId]++;

                    if (plate.ActiveTypes.Count == 3 && HasSameThree(plate.ActiveTypes))
                    {
                        if (i + 1 < typeSushis.Count)
                        {
                            int temp = plate.ActiveTypes[2];
                            plate.ActiveTypes[2] = typeSushis[i + 1];
                            typeSushis[i + 1] = temp;
                        }
                        else if (dispersedSushis.Count > 0)
                        {
                            int temp = plate.ActiveTypes[2];
                            plate.ActiveTypes[2] = dispersedSushis[0];
                            dispersedSushis[0] = temp;
                        }
                        else if (pendingLayerSushis.Count > 0)
                        {
                            int temp = plate.ActiveTypes[2];
                            plate.ActiveTypes[2] = pendingLayerSushis[0];
                            pendingLayerSushis[0] = temp;
                        }
                    }
                }
                else
                {
                    pendingLayerSushis.Add(typeSushis[i]);
                }
            }

            currentPlateIndex += plateSpread;
        }

        Shuffle(dispersedSushis);
        int dispersedIndex = 0;

        for (int i = 0; i < plates.Count; i++)
        {
            if (adPlateIndices.Contains(i)) continue;

            bool isLockedSushiPlate = sushiMergePlateIndices.Contains(i);
            bool isSingleSlot = singleSlotPlateIndices.Contains(i);
            int maxActive = isSingleSlot ? 1 : 3;
            int targetCount = isLockedSushiPlate ? Random.Range(1, maxActive + 1) : maxActive;
            int addedCount = 0;

            while (plates[i].ActiveTypes.Count < targetCount && dispersedIndex < dispersedSushis.Count)
            {
                int typeToAdd = dispersedSushis[dispersedIndex];

                if (!activeTypeCount.ContainsKey(typeToAdd))
                    activeTypeCount[typeToAdd] = 0;

                if (activeTypeCount[typeToAdd] < 3)
                {
                    plates[i].ActiveTypes.Add(typeToAdd);
                    activeTypeCount[typeToAdd]++;
                    dispersedIndex++;
                    addedCount++;
                }
                else
                {
                    pendingLayerSushis.Add(typeToAdd);
                    dispersedIndex++;
                }
            }

            if (addedCount == 0 && plates[i].ActiveTypes.Count == 0 && pendingLayerSushis.Count > 0)
            {
                plates[i].ActiveTypes.Add(pendingLayerSushis[0]);
                pendingLayerSushis.RemoveAt(0);
            }

            if (plates[i].ActiveTypes.Count == 3 && HasSameThree(plates[i].ActiveTypes))
                FixSameThreeInPlate(plates[i], dispersedSushis, ref dispersedIndex);

            int layerCount = Random.Range(levelData.minLayersPerPlate, levelData.maxLayersPerPlate + 1);

            for (int j = 0; j < layerCount && dispersedIndex < dispersedSushis.Count; j++)
            {
                int layerSize = isSingleSlot ? 1 : Random.Range(1, 4);
                var layerTypes = new List<int>();

                for (int k = 0; k < layerSize && dispersedIndex < dispersedSushis.Count; k++)
                    layerTypes.Add(dispersedSushis[dispersedIndex++]);

                if (layerTypes.Count == 3 && HasSameThree(layerTypes))
                {
                    if (dispersedIndex < dispersedSushis.Count)
                    {
                        int temp = layerTypes[2];
                        layerTypes[2] = dispersedSushis[dispersedIndex];
                        dispersedSushis[dispersedIndex] = temp;
                    }
                    else if (pendingLayerSushis.Count > 0 && pendingLayerSushis[0] != layerTypes[0])
                    {
                        int temp = layerTypes[2];
                        layerTypes[2] = pendingLayerSushis[0];
                        pendingLayerSushis[0] = temp;
                    }
                }

                plates[i].Layers.Add(new Layer(layerTypes));
            }
        }

        Shuffle(pendingLayerSushis);
        int pendingIndex = 0;

        while (pendingIndex < pendingLayerSushis.Count)
        {
            for (int i = 0; i < plates.Count && pendingIndex < pendingLayerSushis.Count; i++)
            {
                if (adPlateIndices.Contains(i)) continue;

                bool isSingleSlot = singleSlotPlateIndices.Contains(i);
                var plate = plates[i];

                if (plate.Layers.Count < levelData.maxLayersPerPlate)
                {
                    var newLayer = new List<int>();
                    int layerSize = isSingleSlot ? 1 : Mathf.Min(3, pendingLayerSushis.Count - pendingIndex);

                    for (int j = 0; j < layerSize; j++)
                        newLayer.Add(pendingLayerSushis[pendingIndex++]);

                    if (newLayer.Count == 3 && HasSameThree(newLayer))
                    {
                        if (pendingIndex < pendingLayerSushis.Count && pendingLayerSushis[pendingIndex] != newLayer[0])
                        {
                            int temp = newLayer[2];
                            newLayer[2] = pendingLayerSushis[pendingIndex];
                            pendingLayerSushis[pendingIndex] = temp;
                        }
                        else if (dispersedIndex < dispersedSushis.Count && dispersedSushis[dispersedIndex] != newLayer[0])
                        {
                            int temp = newLayer[2];
                            newLayer[2] = dispersedSushis[dispersedIndex];
                            dispersedSushis[dispersedIndex] = temp;
                        }
                        else
                        {
                            for (int k = 0; k < pendingLayerSushis.Count; k++)
                            {
                                if (pendingLayerSushis[k] != newLayer[0])
                                {
                                    int temp = newLayer[2];
                                    newLayer[2] = pendingLayerSushis[k];
                                    pendingLayerSushis[k] = temp;
                                    break;
                                }
                            }
                        }
                    }

                    plates[i].Layers.Add(new Layer(newLayer));
                }
            }
        }

        while (dispersedIndex < dispersedSushis.Count)
        {
            for (int i = 0; i < plates.Count && dispersedIndex < dispersedSushis.Count; i++)
            {
                if (adPlateIndices.Contains(i)) continue;
                if (sushiMergePlateIndices.Contains(i)) continue;

                bool isSingleSlot = singleSlotPlateIndices.Contains(i);
                int maxActive = isSingleSlot ? 1 : 3;
                var plate = plates[i];

                if (plate.ActiveTypes.Count < maxActive)
                {
                    int typeToAdd = dispersedSushis[dispersedIndex];

                    if (!activeTypeCount.ContainsKey(typeToAdd))
                        activeTypeCount[typeToAdd] = 0;

                    if (activeTypeCount[typeToAdd] < 3)
                    {
                        plate.ActiveTypes.Add(typeToAdd);
                        activeTypeCount[typeToAdd]++;
                        dispersedIndex++;

                        if (plate.ActiveTypes.Count == 3 && HasSameThree(plate.ActiveTypes))
                        {
                            if (dispersedIndex < dispersedSushis.Count && dispersedSushis[dispersedIndex] != plate.ActiveTypes[0])
                            {
                                int temp = plate.ActiveTypes[2];
                                plate.ActiveTypes[2] = dispersedSushis[dispersedIndex];
                                dispersedSushis[dispersedIndex] = temp;
                            }
                            else if (pendingLayerSushis.Count > 0 && pendingLayerSushis[0] != plate.ActiveTypes[0])
                            {
                                int temp = plate.ActiveTypes[2];
                                plate.ActiveTypes[2] = pendingLayerSushis[0];
                                pendingLayerSushis[0] = temp;
                            }
                        }
                    }
                    else
                    {
                        pendingLayerSushis.Add(typeToAdd);
                        dispersedIndex++;
                    }
                }
                else if (plate.Layers.Count < levelData.maxLayersPerPlate)
                {
                    var newLayer = new List<int>();
                    int layerSize = isSingleSlot ? 1 : Mathf.Min(3, dispersedSushis.Count - dispersedIndex);

                    for (int j = 0; j < layerSize; j++)
                        newLayer.Add(dispersedSushis[dispersedIndex++]);

                    if (newLayer.Count == 3 && HasSameThree(newLayer))
                    {
                        if (dispersedIndex < dispersedSushis.Count && dispersedSushis[dispersedIndex] != newLayer[0])
                        {
                            int temp = newLayer[2];
                            newLayer[2] = dispersedSushis[dispersedIndex];
                            dispersedSushis[dispersedIndex] = temp;
                        }
                        else if (pendingLayerSushis.Count > 0 && pendingLayerSushis[0] != newLayer[0])
                        {
                            int temp = newLayer[2];
                            newLayer[2] = pendingLayerSushis[0];
                            pendingLayerSushis[0] = temp;
                        }
                    }

                    plates[i].Layers.Add(new Layer(newLayer));
                }
            }
        }

        EnsureNoEmptyPlates(plates, pendingLayerSushis);
    }
    private void EnsureNoEmptyPlates(List<PlateData> plates, List<int> pendingLayerSushis)
    {
        for (int i = 0; i < plates.Count; i++)
        {
            if (adPlateIndices.Contains(i)) continue;

            var plate = plates[i];
            if (plate.ActiveTypes.Count > 0 || plate.Layers.Count > 0) continue;

            bool isSingleSlot = singleSlotPlateIndices.Contains(i);

            if (pendingLayerSushis.Count > 0)
            {
                int minCount = isSingleSlot ? 1 : Mathf.Min(3, pendingLayerSushis.Count);
                for (int j = 0; j < minCount; j++)
                {
                    plate.ActiveTypes.Add(pendingLayerSushis[0]);
                    pendingLayerSushis.RemoveAt(0);
                }
            }
            else
            {
                for (int j = 0; j < plates.Count; j++)
                {
                    if (j == i || adPlateIndices.Contains(j)) continue;

                    var donorPlate = plates[j];

                    if (donorPlate.Layers.Count > 0)
                    {
                        var layerToMove = donorPlate.Layers[donorPlate.Layers.Count - 1];
                        donorPlate.Layers.RemoveAt(donorPlate.Layers.Count - 1);

                        int moveCount = isSingleSlot ? 1 : layerToMove.SushiTypes.Count;
                        for (int k = 0; k < moveCount; k++)
                            plate.ActiveTypes.Add(layerToMove.SushiTypes[k]);
                        break;
                    }
                    else if (donorPlate.ActiveTypes.Count > 1)
                    {
                        int typeToMove = donorPlate.ActiveTypes[donorPlate.ActiveTypes.Count - 1];
                        donorPlate.ActiveTypes.RemoveAt(donorPlate.ActiveTypes.Count - 1);
                        plate.ActiveTypes.Add(typeToMove);
                        break;
                    }
                }
            }
        }
    }

    private void GeneratePlatesNormal(List<PlateData> plates)
    {
        int index = 0;

        for (int i = 0; i < levelData.plateCount; i++)
        {
            var plateData = new PlateData();
            bool isAdPlate = adPlateIndices.Contains(i);
            bool isLockedSushiPlate = sushiMergePlateIndices.Contains(i);
            bool isSingleSlot = singleSlotPlateIndices.Contains(i);

            if (!isAdPlate)
            {
                int targetCount = 3;
                if (isLockedSushiPlate)
                    targetCount = Random.Range(1, 4);
                else if (isSingleSlot)
                    targetCount = 1;

                if (i < cachedGuaranteedSushis.Count)
                {
                    plateData.ActiveTypes = cachedGuaranteedSushis[i];

                    while (plateData.ActiveTypes.Count < targetCount && index < allSushiTypes.Count)
                        plateData.ActiveTypes.Add(allSushiTypes[index++]);
                }
                else
                {
                    for (int j = 0; j < targetCount && index < allSushiTypes.Count; j++)
                        plateData.ActiveTypes.Add(allSushiTypes[index++]);
                }

                if (HasSameThree(plateData.ActiveTypes))
                    FixSameThree(plateData.ActiveTypes, ref index);
            }

            int layerCount = isAdPlate ? 0 : Random.Range(levelData.minLayersPerPlate, levelData.maxLayersPerPlate + 1);

            for (int j = 0; j < layerCount && index < allSushiTypes.Count; j++)
            {
                int layerSize = isSingleSlot ? 1 : Random.Range(1, 4);
                var layerTypes = new List<int>();

                for (int k = 0; k < layerSize && index < allSushiTypes.Count; k++)
                    layerTypes.Add(allSushiTypes[index++]);

                if (HasSameThree(layerTypes))
                    FixSameThree(layerTypes, ref index);

                plateData.Layers.Add(new Layer(layerTypes));
            }

            plates.Add(plateData);
        }

        DistributeRemainingSushis(plates, ref index);
    }

    private void FixSameThreeInPlate(PlateData plate, List<int> dispersedSushis, ref int dispersedIndex)
    {
        if (plate.ActiveTypes.Count != 3) return;
        if (plate.ActiveTypes[0] != plate.ActiveTypes[1] || plate.ActiveTypes[1] != plate.ActiveTypes[2]) return;

        for (int i = dispersedIndex; i < dispersedSushis.Count; i++)
        {
            if (dispersedSushis[i] != plate.ActiveTypes[0])
            {
                int temp = plate.ActiveTypes[2];
                plate.ActiveTypes[2] = dispersedSushis[i];
                dispersedSushis[i] = temp;
                return;
            }
        }
    }

    private void FixSameThreeInLayer(List<int> layerTypes, List<int> dispersedSushis, ref int dispersedIndex)
    {
        if (layerTypes.Count != 3) return;
        if (layerTypes[0] != layerTypes[1] || layerTypes[1] != layerTypes[2]) return;

        for (int i = dispersedIndex; i < dispersedSushis.Count; i++)
        {
            if (dispersedSushis[i] != layerTypes[0])
            {
                int temp = layerTypes[2];
                layerTypes[2] = dispersedSushis[i];
                dispersedSushis[i] = temp;
                return;
            }
        }
    }

    private void DistributeRemainingSushis(List<PlateData> plates, ref int index)
    {
        if (index >= allSushiTypes.Count) return;

        var availablePlates = new List<int>();
        for (int i = 0; i < plates.Count; i++)
        {
            if (!adPlateIndices.Contains(i))
                availablePlates.Add(i);
        }

        int currentPlateIdx = 0;

        while (index < allSushiTypes.Count && availablePlates.Count > 0)
        {
            int plateIndex = availablePlates[currentPlateIdx % availablePlates.Count];
            var plate = plates[plateIndex];
            bool isSingleSlot = singleSlotPlateIndices.Contains(plateIndex);
            int maxActive = isSingleSlot ? 1 : 3;

            if (plate.ActiveTypes.Count < maxActive)
            {
                plate.ActiveTypes.Add(allSushiTypes[index++]);
            }
            else if (plate.Layers.Count < levelData.maxLayersPerPlate)
            {
                var newLayer = new List<int>();
                int layerSize = isSingleSlot ? 1 : Mathf.Min(3, allSushiTypes.Count - index);

                for (int i = 0; i < layerSize; i++)
                    newLayer.Add(allSushiTypes[index++]);

                if (HasSameThree(newLayer))
                    FixSameThree(newLayer, ref index);

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

    private void CreateInitialEmptySlots(List<PlateData> plates)
    {
        if (levelData.sushiInitEraseCount <= 0) return;

        var typeCountMap = new Dictionary<int, int>();
        for (int i = 0; i < plates.Count; i++)
        {
            if (adPlateIndices.Contains(i)) continue;
            foreach (var typeId in plates[i].ActiveTypes)
            {
                if (!typeCountMap.ContainsKey(typeId)) typeCountMap[typeId] = 0;
                typeCountMap[typeId]++;
            }
            foreach (var layer in plates[i].Layers)
                foreach (var typeId in layer.SushiTypes)
                {
                    if (!typeCountMap.ContainsKey(typeId)) typeCountMap[typeId] = 0;
                    typeCountMap[typeId]++;
                }
        }

        var plateSlotMap = new Dictionary<int, List<int>>();
        for (int i = 0; i < plates.Count; i++)
        {
            if (adPlateIndices.Contains(i)) continue;
            if (sushiMergePlateIndices.Contains(i)) continue;

            plateSlotMap[i] = new List<int>();
            for (int j = 0; j < plates[i].ActiveTypes.Count; j++)
                plateSlotMap[i].Add(j);
        }

        var movedSushis = new List<int>();
        int remainingEraseCount = levelData.sushiInitEraseCount;

        var plateIndices = plateSlotMap.Keys.ToList();
        Shuffle(plateIndices);

        foreach (var plateIndex in plateIndices)
        {
            if (remainingEraseCount <= 0) break;

            var slots = plateSlotMap[plateIndex];
            if (slots.Count == 0) continue;

            int maxRemovable = Mathf.Min(slots.Count - 1, 2);
            if (maxRemovable <= 0) continue;

            int removeCount = Mathf.Min(Random.Range(1, maxRemovable + 1), remainingEraseCount);

            Shuffle(slots);

            for (int i = 0; i < removeCount; i++)
            {
                int slotIndex = slots[i];
                int sushiType = plates[plateIndex].ActiveTypes[slotIndex];

                if (typeCountMap[sushiType] <= 3) continue;
                if ((typeCountMap[sushiType] - 1) % 3 != 0)
                {
                    movedSushis.Add(sushiType);
                    plates[plateIndex].ActiveTypes[slotIndex] = -1;
                    typeCountMap[sushiType]--;
                    remainingEraseCount--;
                }
            }
        }

        for (int i = 0; i < plates.Count; i++)
        {
            if (adPlateIndices.Contains(i)) continue;
            if (sushiMergePlateIndices.Contains(i)) continue;
            plates[i].ActiveTypes.RemoveAll(typeId => typeId == -1);
        }

        DistributeMovedSushisToReserve(plates, movedSushis);
    }

    private void DistributeMovedSushisToReserve(List<PlateData> plates, List<int> movedSushis)
    {
        if (movedSushis.Count == 0) return;

        var availablePlates = new List<int>();
        for (int i = 0; i < plates.Count; i++)
        {
            if (!adPlateIndices.Contains(i) && plates[i].Layers.Count < levelData.maxLayersPerPlate)
                availablePlates.Add(i);
        }

        if (availablePlates.Count == 0)
        {
            var firstNonAdPlate = -1;
            for (int i = 0; i < plates.Count; i++)
            {
                if (!adPlateIndices.Contains(i))
                {
                    firstNonAdPlate = i;
                    break;
                }
            }

            if (firstNonAdPlate >= 0)
                plates[firstNonAdPlate].Layers.Add(new Layer(movedSushis));
            return;
        }

        Shuffle(availablePlates);

        int sushiIndex = 0;
        int plateIndex = 0;

        while (sushiIndex < movedSushis.Count)
        {
            int targetPlateIndex = availablePlates[plateIndex % availablePlates.Count];
            var targetPlate = plates[targetPlateIndex];
            bool isSingleSlot = singleSlotPlateIndices.Contains(targetPlateIndex);

            if (targetPlate.Layers.Count >= levelData.maxLayersPerPlate)
            {
                availablePlates.RemoveAt(plateIndex % availablePlates.Count);
                if (availablePlates.Count == 0) break;
                continue;
            }

            var newLayer = new List<int>();
            int layerSize = isSingleSlot ? 1 : Mathf.Min(3, movedSushis.Count - sushiIndex);

            for (int i = 0; i < layerSize; i++)
                newLayer.Add(movedSushis[sushiIndex++]);

            if (HasSameThree(newLayer))
            {
                if (sushiIndex < movedSushis.Count)
                {
                    int temp = newLayer[2];
                    newLayer[2] = movedSushis[sushiIndex];
                    movedSushis[sushiIndex] = temp;
                }
            }

            targetPlate.Layers.Add(new Layer(newLayer));
            plateIndex++;
        }
    }

    private void DetermineLockedPlates()
    {
        adPlateIndices.Clear();
        sushiMergePlateIndices.Clear();
        singleSlotPlateIndices.Clear();

        int guaranteedPlateCount = cachedGuaranteedSushis.Count;

        var availablePlates = new List<int>();
        for (int i = guaranteedPlateCount; i < levelData.plateCount; i++)
            availablePlates.Add(i);

        Shuffle(availablePlates);

        int mergeUnlockCount = Mathf.Min(levelData.mergeUnlockCount, levelData.lockedPlateCount);
        int totalLockedCount = Mathf.Min(levelData.lockedPlateCount, availablePlates.Count);

        for (int i = 0; i < mergeUnlockCount && i < totalLockedCount; i++)
            sushiMergePlateIndices.Add(availablePlates[i]);

        for (int i = mergeUnlockCount; i < totalLockedCount; i++)
            adPlateIndices.Add(availablePlates[i]);

        var remainingPlates = availablePlates
            .Skip(totalLockedCount)
            .Where(i => !singleSlotPlateIndices.Contains(i))
            .ToList();

        int singleSlotCount = Mathf.Min(levelData.singleSlotPlateCount, remainingPlates.Count);
        for (int i = 0; i < singleSlotCount; i++)
            singleSlotPlateIndices.Add(remainingPlates[i]);
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
        var availableSushiTypes = GetAvailableSushiTypes(plates);
        Shuffle(availableSushiTypes);

        int typeIndex = 0;
        foreach (var plateIndex in sushiMergePlateIndices)
        {
            if (typeIndex >= availableSushiTypes.Count)
            {
                Debug.LogWarning("[LevelGenerator] LockedSushi 플레이트보다 사용 가능한 초밥 타입이 부족합니다.");
                break;
            }

            int sushiType = availableSushiTypes[typeIndex++];
            plates[plateIndex].State = PlateState.LockedSushi;
            plates[plateIndex].RequiredSushiTypeId = sushiType;
        }

        foreach (var plateIndex in adPlateIndices)
            plates[plateIndex].State = PlateState.LockedAd;

        foreach (var plateIndex in singleSlotPlateIndices)
            plates[plateIndex].SlotCount = 1;

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
    public int SlotCount = 3;

    public PlateData()
    {
        ActiveTypes = new List<int>();
        Layers = new List<Layer>();
    }
}