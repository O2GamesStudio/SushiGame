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
    private HashSet<int> guaranteedPlateIndices;
    private HashSet<(int plateIndex, int slotIndex)> guaranteedSlots;
    private HashSet<int> railPlateIndices;
    private RailData railData;
    private Dictionary<int, int> eraseCountPerPlate; // 클래스 필드로 승격 - EnsureNoEmptyPlates에서 접근 필요

    public LevelGenerator(LevelData data)
    {
        levelData = data;
        adPlateIndices = new HashSet<int>();
        sushiMergePlateIndices = new HashSet<int>();
        singleSlotPlateIndices = new HashSet<int>();
        concentratedTypes = new HashSet<int>();
        guaranteedPlateIndices = new HashSet<int>();
        guaranteedSlots = new HashSet<(int, int)>();
        railPlateIndices = new HashSet<int>();
        eraseCountPerPlate = new Dictionary<int, int>();
        SelectRandomSushiTypes();
        GenerateSushiPool();

        if (levelData.isRail)
            InitializeRail();
    }

    public RailData GetRailData() => railData;

    private void InitializeRail()
    {
        int rowCount = levelData.plateCount / 3;
        int railRow = Random.Range(0, rowCount);
        int rowStart = railRow * 3;

        railPlateIndices.Add(rowStart);
        railPlateIndices.Add(rowStart + 1);
        railPlateIndices.Add(rowStart + 2);

        int railSetCount = Mathf.Max(1, levelData.railSushiCount / 3);
        var railSushiTypeIds = new List<int>();

        var typeGroups = new Dictionary<int, List<int>>();
        for (int i = 0; i < allSushiTypes.Count; i++)
        {
            int t = allSushiTypes[i];
            if (!typeGroups.ContainsKey(t)) typeGroups[t] = new List<int>();
            typeGroups[t].Add(i);
        }

        var availableTypes = new List<int>(typeGroups.Keys);
        Shuffle(availableTypes);

        var indicesToRemove = new List<int>();

        foreach (int typeId in availableTypes)
        {
            if (railSushiTypeIds.Count >= railSetCount * 3) break;
            if (typeGroups[typeId].Count < 3) continue;

            for (int i = 0; i < 3; i++)
            {
                railSushiTypeIds.Add(typeId);
                indicesToRemove.Add(typeGroups[typeId][i]);
            }
        }

        indicesToRemove.Sort((a, b) => b.CompareTo(a));
        foreach (int idx in indicesToRemove)
            allSushiTypes.RemoveAt(idx);

        Shuffle(railSushiTypeIds);

        railData = new RailData
        {
            RowIndex = railRow,
            SushiTypeIds = railSushiTypeIds,
            RailPlateSprite = levelData.railPlateSprite
        };
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
            concentratedTypes.Add(shuffled[i]);
    }

    private void GenerateSushiPool()
    {
        allSushiTypes = new List<int>();

        int totalSushiCount = levelData.totalSushiSetCount * 3;
        int basePerType = (totalSushiCount / levelData.sushiTypeCount / 3) * 3;

        foreach (var typeId in selectedSushiTypes)
            for (int i = 0; i < basePerType; i++)
                allSushiTypes.Add(typeId);

        int remaining = totalSushiCount - allSushiTypes.Count;
        int typeIndex = 0;

        while (remaining > 0)
        {
            int addCount = Mathf.Min(3, remaining);
            for (int i = 0; i < addCount; i++)
                allSushiTypes.Add(selectedSushiTypes[typeIndex]);
            remaining -= addCount;
            typeIndex = (typeIndex + 1) % selectedSushiTypes.Count;
        }

        Shuffle(allSushiTypes);
    }

    // erase 분배를 클래스 필드에 계산 - GeneratePlatesNormal/WithConcentration 공유
    private void ComputeEraseCountPerPlate(int effectivePlateCount)
    {
        eraseCountPerPlate.Clear();
        if (levelData.sushiInitEraseCount <= 0) return;

        var erasablePlateIndices = new List<int>();
        for (int i = 0; i < effectivePlateCount; i++)
        {
            if (adPlateIndices.Contains(i) || sushiMergePlateIndices.Contains(i) || singleSlotPlateIndices.Contains(i)) continue;
            erasablePlateIndices.Add(i);
        }
        Shuffle(erasablePlateIndices);

        int eraseRemaining = levelData.sushiInitEraseCount;
        int eraseIdx = 0;
        while (eraseRemaining > 0 && erasablePlateIndices.Count > 0)
        {
            int targetPlate = erasablePlateIndices[eraseIdx % erasablePlateIndices.Count];
            if (!eraseCountPerPlate.ContainsKey(targetPlate))
                eraseCountPerPlate[targetPlate] = 0;

            if (eraseCountPerPlate[targetPlate] < 2)
            {
                eraseCountPerPlate[targetPlate]++;
                eraseRemaining--;
            }
            eraseIdx++;

            if (eraseIdx >= erasablePlateIndices.Count * 2) break;
        }
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

        AssignLockedPlates(plates);

        if (levelData.hiddenReserveCount > 0)
            ApplyHiddenReserves(plates);

        FixTypeMultiples(plates);
        EnforceMinLayers(plates);
        ValidatePlates(plates);

        return plates;
    }

    private int GetEffectivePlateCount() => levelData.plateCount - railPlateIndices.Count;

    private int GetWeightedLayerSize()
    {
        int total = levelData.layerSize1Weight + levelData.layerSize2Weight + levelData.layerSize3Weight;
        if (total <= 0) return Random.Range(1, 4);

        int roll = Random.Range(0, total);
        if (roll < levelData.layerSize1Weight) return 1;
        if (roll < levelData.layerSize1Weight + levelData.layerSize2Weight) return 2;
        return 3;
    }

    private int CountTypeInReserve(PlateData plate, int typeId, List<int> currentLayer)
    {
        int count = 0;
        foreach (var layer in plate.Layers)
            foreach (var t in layer.SushiTypes)
                if (t == typeId) count++;
        count += currentLayer.Count(t => t == typeId);
        return count;
    }

    private bool TrySwapCandidate(List<int> pool, int poolIndex, PlateData plate, List<int> currentLayer)
    {
        int candidate = pool[poolIndex];
        if (CountTypeInReserve(plate, candidate, currentLayer) < 2) return true;

        for (int s = poolIndex + 1; s < pool.Count; s++)
        {
            int alt = pool[s];
            if (CountTypeInReserve(plate, alt, currentLayer) < 2)
            {
                (pool[poolIndex], pool[s]) = (pool[s], pool[poolIndex]);
                return true;
            }
        }
        return false;
    }

    private List<int> BuildLayer(List<int> pool, ref int poolIndex, PlateData plate, bool isSingleSlot)
    {
        if (poolIndex >= pool.Count) return null;

        int layerSize = isSingleSlot ? 1 : GetWeightedLayerSize();
        var layerTypes = new List<int>(layerSize);

        layerTypes.Add(pool[poolIndex++]);

        for (int k = 1; k < layerSize && poolIndex < pool.Count; k++)
        {
            if (!TrySwapCandidate(pool, poolIndex, plate, layerTypes)) break;
            layerTypes.Add(pool[poolIndex++]);
        }

        return layerTypes;
    }

    private void DistributeLayersToPlates(List<PlateData> plates, List<int> validPlateIndices, List<int> pool, ref int poolIndex)
    {
        for (int minLayer = 0; minLayer < levelData.minLayersPerPlate; minLayer++)
        {
            foreach (int i in validPlateIndices)
            {
                if (poolIndex >= pool.Count) break;
                bool isSingleSlot = singleSlotPlateIndices.Contains(i);
                var layerTypes = BuildLayer(pool, ref poolIndex, plates[i], isSingleSlot);
                if (layerTypes != null && layerTypes.Count > 0)
                    plates[i].Layers.Add(new Layer(layerTypes));
            }
        }

        var shuffledPlates = new List<int>(validPlateIndices);
        while (poolIndex < pool.Count)
        {
            Shuffle(shuffledPlates);
            bool anyAdded = false;

            foreach (int i in shuffledPlates)
            {
                if (poolIndex >= pool.Count) break;
                if (plates[i].Layers.Count >= levelData.maxLayersPerPlate) continue;

                bool isSingleSlot = singleSlotPlateIndices.Contains(i);
                var layerTypes = BuildLayer(pool, ref poolIndex, plates[i], isSingleSlot);
                if (layerTypes == null || layerTypes.Count == 0) continue;

                if (HasSameThree(layerTypes))
                {
                    int tmp = poolIndex;
                    FixSameThree(layerTypes, ref tmp);
                    poolIndex = tmp;
                }

                plates[i].Layers.Add(new Layer(layerTypes));
                anyAdded = true;
            }

            if (!anyAdded) break;
        }
    }

    private List<int> GetValidPlateIndices(List<PlateData> plates)
    {
        var result = new List<int>();
        for (int i = 0; i < plates.Count; i++)
            if (!adPlateIndices.Contains(i))
                result.Add(i);
        return result;
    }

    private void GeneratePlatesNormal(List<PlateData> plates)
    {
        int effectivePlateCount = GetEffectivePlateCount();
        int index = 0;

        // 로컬 변수 제거 - 클래스 필드 사용
        ComputeEraseCountPerPlate(effectivePlateCount);

        for (int i = 0; i < effectivePlateCount; i++)
        {
            var plateData = new PlateData();

            if (!adPlateIndices.Contains(i))
            {
                bool isLockedSushi = sushiMergePlateIndices.Contains(i);
                bool isSingleSlot = singleSlotPlateIndices.Contains(i);
                int targetCount = isLockedSushi ? Random.Range(1, 4) : (isSingleSlot ? 1 : 3);

                if (eraseCountPerPlate.ContainsKey(i))
                    targetCount = Mathf.Max(1, targetCount - eraseCountPerPlate[i]);

                if (i < cachedGuaranteedSushis.Count)
                {
                    plateData.ActiveTypes = cachedGuaranteedSushis[i];
                    for (int s = 0; s < plateData.ActiveTypes.Count; s++)
                        guaranteedSlots.Add((i, s));

                    while (plateData.ActiveTypes.Count < targetCount && index < allSushiTypes.Count)
                    {
                        if (plateData.ActiveTypes.Count == 2 &&
                            plateData.ActiveTypes[0] == plateData.ActiveTypes[1] &&
                            allSushiTypes[index] == plateData.ActiveTypes[0])
                        {
                            bool swapped = false;
                            for (int k = index + 1; k < allSushiTypes.Count; k++)
                            {
                                if (allSushiTypes[k] != plateData.ActiveTypes[0])
                                {
                                    (allSushiTypes[index], allSushiTypes[k]) = (allSushiTypes[k], allSushiTypes[index]);
                                    swapped = true;
                                    break;
                                }
                            }
                            if (!swapped) break;
                        }
                        plateData.ActiveTypes.Add(allSushiTypes[index++]);
                    }
                    guaranteedPlateIndices.Add(i);
                }
                else
                {
                    for (int j = 0; j < targetCount && index < allSushiTypes.Count; j++)
                    {
                        if (j == 2 && plateData.ActiveTypes.Count == 2 &&
                            plateData.ActiveTypes[0] == plateData.ActiveTypes[1] &&
                            index < allSushiTypes.Count &&
                            allSushiTypes[index] == plateData.ActiveTypes[0])
                        {
                            bool swapped = false;
                            for (int k = index + 1; k < allSushiTypes.Count; k++)
                            {
                                if (allSushiTypes[k] != plateData.ActiveTypes[0])
                                {
                                    (allSushiTypes[index], allSushiTypes[k]) = (allSushiTypes[k], allSushiTypes[index]);
                                    swapped = true;
                                    break;
                                }
                            }
                            if (!swapped) break;
                        }
                        plateData.ActiveTypes.Add(allSushiTypes[index++]);
                    }
                }
            }

            plates.Add(plateData);
        }

        var validPlateIndices = GetValidPlateIndices(plates);
        DistributeLayersToPlates(plates, validPlateIndices, allSushiTypes, ref index);
    }

    private void GeneratePlatesWithConcentration(List<PlateData> plates)
    {
        int effectivePlateCount = GetEffectivePlateCount();

        // 로컬 변수 제거 - 클래스 필드 사용
        ComputeEraseCountPerPlate(effectivePlateCount);

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

        for (int i = 0; i < effectivePlateCount; i++)
            plates.Add(new PlateData());

        int currentPlateIndex = 0;
        var activeTypeCount = new Dictionary<int, int>();

        foreach (var plateTypes in cachedGuaranteedSushis)
        {
            plates[currentPlateIndex].ActiveTypes = new List<int>(plateTypes);
            for (int s = 0; s < plateTypes.Count; s++)
                guaranteedSlots.Add((currentPlateIndex, s));
            foreach (var typeId in plateTypes)
            {
                if (!activeTypeCount.ContainsKey(typeId)) activeTypeCount[typeId] = 0;
                activeTypeCount[typeId]++;
            }
            guaranteedPlateIndices.Add(currentPlateIndex);
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

            if (!activeTypeCount.ContainsKey(typeId)) activeTypeCount[typeId] = 0;

            int targetPlateStart = currentPlateIndex % effectivePlateCount;

            for (int i = 0; i < typeSushis.Count; i++)
            {
                int targetPlate = (targetPlateStart + (i / 2)) % effectivePlateCount;
                int attempts = 0;

                while (adPlateIndices.Contains(targetPlate) && attempts < effectivePlateCount)
                {
                    targetPlate = (targetPlate + 1) % effectivePlateCount;
                    attempts++;
                }

                if (attempts >= effectivePlateCount)
                {
                    pendingLayerSushis.Add(typeSushis[i]);
                    continue;
                }

                var plate = plates[targetPlate];
                bool isSingleSlot = singleSlotPlateIndices.Contains(targetPlate);
                int maxActive = isSingleSlot ? 1 : 3;
                int eraseCount = eraseCountPerPlate.ContainsKey(targetPlate) ? eraseCountPerPlate[targetPlate] : 0;
                int effectiveMax = Mathf.Max(1, maxActive - eraseCount);
                bool canAddToActive = plate.ActiveTypes.Count < effectiveMax && activeTypeCount[typeId] < maxActive;

                if (canAddToActive)
                {
                    plate.ActiveTypes.Add(typeSushis[i]);
                    activeTypeCount[typeId]++;

                    if (plate.ActiveTypes.Count == 3 && HasSameThree(plate.ActiveTypes))
                    {
                        if (i + 1 < typeSushis.Count)
                            (plate.ActiveTypes[2], typeSushis[i + 1]) = (typeSushis[i + 1], plate.ActiveTypes[2]);
                        else if (dispersedSushis.Count > 0)
                            (plate.ActiveTypes[2], dispersedSushis[0]) = (dispersedSushis[0], plate.ActiveTypes[2]);
                    }
                }
                else
                {
                    pendingLayerSushis.Add(typeSushis[i]);
                }
            }

            currentPlateIndex++;
        }

        Shuffle(dispersedSushis);
        int dispersedIndex = 0;

        for (int i = 0; i < plates.Count; i++)
        {
            if (adPlateIndices.Contains(i)) continue;

            bool isLockedSushi = sushiMergePlateIndices.Contains(i);
            bool isSingleSlot = singleSlotPlateIndices.Contains(i);
            int maxActive = isSingleSlot ? 1 : 3;
            int eraseCount = eraseCountPerPlate.ContainsKey(i) ? eraseCountPerPlate[i] : 0;
            int targetCount = isLockedSushi ? Random.Range(1, maxActive + 1) : Mathf.Max(1, maxActive - eraseCount);

            while (plates[i].ActiveTypes.Count < targetCount && dispersedIndex < dispersedSushis.Count)
            {
                int typeToAdd = dispersedSushis[dispersedIndex];
                if (!activeTypeCount.ContainsKey(typeToAdd)) activeTypeCount[typeToAdd] = 0;

                if (activeTypeCount[typeToAdd] < 3)
                {
                    plates[i].ActiveTypes.Add(typeToAdd);
                    activeTypeCount[typeToAdd]++;
                }
                else
                {
                    pendingLayerSushis.Add(typeToAdd);
                }
                dispersedIndex++;
            }

            if (plates[i].ActiveTypes.Count == 3 && HasSameThree(plates[i].ActiveTypes))
                FixSameThreeInPlate(plates[i], dispersedSushis, ref dispersedIndex);
        }

        while (dispersedIndex < dispersedSushis.Count)
            pendingLayerSushis.Add(dispersedSushis[dispersedIndex++]);

        Shuffle(pendingLayerSushis);

        var validPlateIndices = GetValidPlateIndices(plates);
        int poolIndex = 0;
        DistributeLayersToPlates(plates, validPlateIndices, pendingLayerSushis, ref poolIndex);
        EnsureNoEmptyPlates(plates, pendingLayerSushis.Skip(poolIndex).ToList());
    }

    private void EnsureNoEmptyPlates(List<PlateData> plates, List<int> pendingLayerSushis)
    {
        for (int i = 0; i < plates.Count; i++)
        {
            if (adPlateIndices.Contains(i)) continue;

            var plate = plates[i];
            bool isSingleSlot = singleSlotPlateIndices.Contains(i);
            int maxActive = isSingleSlot ? 1 : 3;

            // erase가 적용된 실제 상한 계산 - 이게 없으면 eraseCount를 무시하고 3개로 채워버림
            int eraseCount = eraseCountPerPlate.ContainsKey(i) ? eraseCountPerPlate[i] : 0;
            int effectiveMax = Mathf.Max(1, maxActive - eraseCount);

            if (plate.ActiveTypes.Count == 0)
            {
                if (plate.Layers.Count > 0)
                {
                    var firstLayer = plate.Layers[0];
                    int moveCount = Mathf.Min(effectiveMax, firstLayer.SushiTypes.Count);
                    for (int k = 0; k < moveCount; k++)
                        plate.ActiveTypes.Add(firstLayer.SushiTypes[k]);
                    firstLayer.SushiTypes.RemoveRange(0, moveCount);
                    if (firstLayer.SushiTypes.Count == 0)
                        plate.Layers.RemoveAt(0);
                }
                else if (pendingLayerSushis.Count > 0)
                {
                    int minCount = Mathf.Min(effectiveMax, pendingLayerSushis.Count);
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
                        var donor = plates[j];

                        if (CanDonateLayer(j, plates))
                        {
                            var layerToMove = donor.Layers[donor.Layers.Count - 1];
                            donor.Layers.RemoveAt(donor.Layers.Count - 1);
                            int moveCount = Mathf.Min(effectiveMax, layerToMove.SushiTypes.Count);
                            for (int k = 0; k < moveCount; k++)
                                plate.ActiveTypes.Add(layerToMove.SushiTypes[k]);
                            // 이동 못한 나머지는 레이어로 돌려보냄
                            if (layerToMove.SushiTypes.Count > moveCount)
                                donor.Layers.Add(new Layer(layerToMove.SushiTypes.GetRange(moveCount, layerToMove.SushiTypes.Count - moveCount)));
                            break;
                        }
                        else if (donor.ActiveTypes.Count > 1)
                        {
                            int typeToMove = donor.ActiveTypes[donor.ActiveTypes.Count - 1];
                            donor.ActiveTypes.RemoveAt(donor.ActiveTypes.Count - 1);
                            plate.ActiveTypes.Add(typeToMove);
                            break;
                        }
                    }
                }
            }

            if (plate.ActiveTypes.Count == 0 && plate.Layers.Count == 0 && pendingLayerSushis.Count > 0)
            {
                int minCount = isSingleSlot ? 1 : Mathf.Min(effectiveMax, pendingLayerSushis.Count);
                for (int j = 0; j < minCount; j++)
                {
                    plate.ActiveTypes.Add(pendingLayerSushis[0]);
                    pendingLayerSushis.RemoveAt(0);
                }
            }
        }
    }

    private bool CanDonateLayer(int plateIndex, List<PlateData> plates)
    {
        if (adPlateIndices.Contains(plateIndex) || sushiMergePlateIndices.Contains(plateIndex)) return false;
        return plates[plateIndex].Layers.Count > levelData.minLayersPerPlate;
    }

    private void EnforceMinLayers(List<PlateData> plates)
    {
        if (levelData.minLayersPerPlate <= 0) return;

        for (int i = 0; i < plates.Count; i++)
        {
            if (adPlateIndices.Contains(i) || sushiMergePlateIndices.Contains(i)) continue;

            while (plates[i].Layers.Count < levelData.minLayersPerPlate)
            {
                bool donated = false;
                for (int j = 0; j < plates.Count; j++)
                {
                    if (j == i || adPlateIndices.Contains(j)) continue;
                    if (plates[j].Layers.Count > levelData.minLayersPerPlate)
                    {
                        var layer = plates[j].Layers[plates[j].Layers.Count - 1];
                        plates[j].Layers.RemoveAt(plates[j].Layers.Count - 1);
                        plates[i].Layers.Add(layer);
                        donated = true;
                        break;
                    }
                }
                if (!donated) break;
            }
        }
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

        if (railData != null)
        {
            foreach (var typeId in railData.SushiTypeIds)
            {
                if (!typeCountMap.ContainsKey(typeId)) typeCountMap[typeId] = 0;
                typeCountMap[typeId]++;
            }
        }

        foreach (var kvp in typeCountMap)
        {
            int typeId = kvp.Key;
            int remainder = kvp.Value % 3;
            if (remainder == 0) continue;

            int toAdd = 3 - remainder;
            for (int added = 0; added < toAdd; added++)
            {
                // 항상 레이어에만 추가 - active slot에는 절대 추가하지 않음
                // active에 추가하면 sushiInitEraseCount로 비워둔 슬롯이 복구되어버림
                // maxLayersPerPlate 제한 무시 - 타입 균형이 우선
                for (int i = 0; i < plates.Count; i++)
                {
                    if (adPlateIndices.Contains(i)) continue;
                    plates[i].Layers.Add(new Layer(new List<int> { typeId }));
                    break;
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
                    allReserveIndices.Add((plate, layerIdx, sushiIdx));
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
    }

    private void DetermineLockedPlates()
    {
        adPlateIndices.Clear();
        sushiMergePlateIndices.Clear();
        singleSlotPlateIndices.Clear();

        int effectivePlateCount = GetEffectivePlateCount();
        int guaranteedPlateCount = cachedGuaranteedSushis.Count;
        var availablePlates = new List<int>();
        for (int i = guaranteedPlateCount; i < effectivePlateCount; i++)
            availablePlates.Add(i);

        Shuffle(availablePlates);

        int mergeUnlockCount = Mathf.Min(levelData.mergeUnlockCount, levelData.lockedPlateCount);
        int totalLockedCount = Mathf.Min(levelData.lockedPlateCount, availablePlates.Count);

        for (int i = 0; i < mergeUnlockCount && i < totalLockedCount; i++)
            sushiMergePlateIndices.Add(availablePlates[i]);

        for (int i = mergeUnlockCount; i < totalLockedCount; i++)
            adPlateIndices.Add(availablePlates[i]);

        var remainingPlates = availablePlates.Skip(totalLockedCount)
            .Where(i => !singleSlotPlateIndices.Contains(i)).ToList();

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
                foreach (var typeId in layer.SushiTypes)
                {
                    totalSushis++;
                    if (!typeCount.ContainsKey(typeId)) typeCount[typeId] = 0;
                    typeCount[typeId]++;
                }
        }

        if (railData != null)
        {
            foreach (var typeId in railData.SushiTypeIds)
            {
                totalSushis++;
                if (!typeCount.ContainsKey(typeId)) typeCount[typeId] = 0;
                typeCount[typeId]++;
            }
        }

        Debug.Log($"[LevelGenerator] 배치된 총 초밥: {totalSushis}, 3의 배수: {totalSushis % 3 == 0}");
        foreach (var kvp in typeCount)
        {
            if (kvp.Value % 3 != 0)
                Debug.LogError($"[LevelGenerator] 타입 {kvp.Key}: {kvp.Value}개 - 3의 배수 아님!");
        }
        if (totalSushis % 3 != 0)
            Debug.LogError($"[LevelGenerator] 초밥 총합이 3의 배수 아님! ({totalSushis}개)");
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
            plates[plateIndex].State = PlateState.LockedSushi;
            plates[plateIndex].RequiredSushiTypeId = availableSushiTypes[typeIndex++];
        }

        foreach (var plateIndex in adPlateIndices)
            plates[plateIndex].State = PlateState.LockedAd;

        foreach (var plateIndex in singleSlotPlateIndices)
            plates[plateIndex].SlotCount = 1;

        AssignLockedSushis(plates);
        ResolveLockedPlateDeadlocks(plates);
    }

    private void ResolveLockedPlateDeadlocks(List<PlateData> plates)
    {
        foreach (var plateIndex in sushiMergePlateIndices)
        {
            var plate = plates[plateIndex];
            int requiredType = plate.RequiredSushiTypeId;
            if (requiredType < 0) continue;

            for (int i = plate.ActiveTypes.Count - 1; i >= 0; i--)
            {
                if (plate.ActiveTypes[i] != requiredType) continue;

                bool moved = false;
                for (int otherIdx = 0; otherIdx < plates.Count; otherIdx++)
                {
                    if (otherIdx == plateIndex || adPlateIndices.Contains(otherIdx)) continue;
                    if (plates[otherIdx].Layers.Count >= levelData.maxLayersPerPlate) continue;

                    plates[otherIdx].Layers.Add(new Layer(new List<int> { requiredType }));
                    plate.ActiveTypes.RemoveAt(i);
                    moved = true;
                    break;
                }

                if (!moved)
                {
                    for (int otherIdx = 0; otherIdx < plates.Count; otherIdx++)
                    {
                        if (otherIdx == plateIndex || adPlateIndices.Contains(otherIdx)) continue;
                        plates[otherIdx].Layers.Add(new Layer(new List<int> { requiredType }));
                        plate.ActiveTypes.RemoveAt(i);
                        break;
                    }
                }
            }

            for (int layerIdx = plate.Layers.Count - 1; layerIdx >= 0; layerIdx--)
            {
                var layer = plate.Layers[layerIdx];
                var indicesToMove = new List<int>();

                for (int sushiIdx = 0; sushiIdx < layer.SushiTypes.Count; sushiIdx++)
                    if (layer.SushiTypes[sushiIdx] == requiredType)
                        indicesToMove.Add(sushiIdx);

                if (indicesToMove.Count == 0) continue;

                bool moved = false;
                for (int otherIdx = 0; otherIdx < plates.Count; otherIdx++)
                {
                    if (otherIdx == plateIndex || adPlateIndices.Contains(otherIdx)) continue;
                    if (plates[otherIdx].Layers.Count >= levelData.maxLayersPerPlate) continue;

                    plates[otherIdx].Layers.Add(new Layer(new List<int>(
                        indicesToMove.Select(_ => requiredType))));

                    foreach (var idx in indicesToMove.OrderByDescending(x => x))
                        layer.SushiTypes.RemoveAt(idx);

                    if (layer.SushiTypes.Count == 0)
                        plate.Layers.RemoveAt(layerIdx);

                    moved = true;
                    break;
                }

                if (!moved)
                {
                    for (int otherIdx = 0; otherIdx < plates.Count; otherIdx++)
                    {
                        if (otherIdx == plateIndex || adPlateIndices.Contains(otherIdx)) continue;

                        plates[otherIdx].Layers.Add(new Layer(new List<int>(
                            indicesToMove.Select(_ => requiredType))));

                        foreach (var idx in indicesToMove.OrderByDescending(x => x))
                            layer.SushiTypes.RemoveAt(idx);

                        if (layer.SushiTypes.Count == 0)
                            plate.Layers.RemoveAt(layerIdx);

                        break;
                    }
                }
            }
        }
    }

    private void AssignLockedSushis(List<PlateData> plates)
    {
        if (levelData.lockedSushiCount <= 0) return;

        var availableSlots = new List<(int plateIndex, int slotIndex, bool isActive, int layerIndex)>();

        for (int i = 0; i < plates.Count; i++)
        {
            if (plates[i].State != PlateState.Normal) continue;

            for (int j = 0; j < plates[i].ActiveTypes.Count; j++)
                availableSlots.Add((i, j, true, -1));

            for (int layerIdx = 0; layerIdx < plates[i].Layers.Count; layerIdx++)
            {
                var layer = plates[i].Layers[layerIdx];
                for (int j = 0; j < layer.Count; j++)
                    availableSlots.Add((i, j, false, layerIdx));
            }
        }

        Shuffle(availableSlots);
        int lockedCount = Mathf.Min(levelData.lockedSushiCount, availableSlots.Count);

        for (int i = 0; i < lockedCount; i++)
        {
            var slot = availableSlots[i];
            if (slot.isActive)
                plates[slot.plateIndex].ActiveLockStages[slot.slotIndex] = 3;
            else
                plates[slot.plateIndex].Layers[slot.layerIndex].SetLockStage(slot.slotIndex, 3);
        }
    }

    private List<List<int>> ExtractGuaranteedSushis()
    {
        var result = new List<List<int>>();
        if (levelData.guaranteedMergeSets <= 0) return result;

        var typeCount = new Dictionary<int, int>();
        foreach (var typeId in allSushiTypes)
        {
            if (!typeCount.ContainsKey(typeId)) typeCount[typeId] = 0;
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
                allSushiTypes.RemoveAt(idx);

            var possibleDistributions = new List<List<int>>
            {
                new List<int> { 1, 2 },
                new List<int> { 2, 1 },
                new List<int> { 1, 1, 1 }
            };
            var distribution = possibleDistributions[Random.Range(0, possibleDistributions.Count)];

            foreach (var count in distribution)
            {
                var plateTypes = new List<int>(count);
                for (int i = 0; i < count; i++)
                    plateTypes.Add(typeId);
                result.Add(plateTypes);
            }
        }

        Shuffle(result);
        return result;
    }

    private List<int> GetAvailableSushiTypes(List<PlateData> plates)
    {
        var sushiTypes = new HashSet<int>();
        foreach (var plate in plates)
        {
            foreach (var typeId in plate.ActiveTypes) sushiTypes.Add(typeId);
            foreach (var layer in plate.Layers)
                foreach (var typeId in layer.SushiTypes) sushiTypes.Add(typeId);
        }
        return sushiTypes.ToList();
    }

    private void FixSameThreeInPlate(PlateData plate, List<int> dispersedSushis, ref int dispersedIndex)
    {
        if (plate.ActiveTypes.Count != 3) return;
        if (plate.ActiveTypes[0] != plate.ActiveTypes[1] || plate.ActiveTypes[1] != plate.ActiveTypes[2]) return;

        for (int i = dispersedIndex; i < dispersedSushis.Count; i++)
        {
            if (dispersedSushis[i] != plate.ActiveTypes[0])
            {
                (plate.ActiveTypes[2], dispersedSushis[i]) = (dispersedSushis[i], plate.ActiveTypes[2]);
                return;
            }
        }
    }

    private bool HasSameThree(List<int> types)
    {
        if (types.Count != 3) return false;
        return types[0] == types[1] && types[1] == types[2];
    }

    private void FixSameThree(List<int> types, ref int currentIndex)
    {
        if (types.Count != 3) return;
        if (types[0] != types[1] || types[1] != types[2]) return;

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

        foreach (var typeId in selectedSushiTypes)
        {
            if (typeId != types[0])
            {
                types[2] = typeId;
                allSushiTypes.Add(types[0]);
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