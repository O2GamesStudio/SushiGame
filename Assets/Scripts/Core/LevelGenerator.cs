using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelGenerator
{
    #region Fields
    private readonly LevelData levelData;
    private List<int> allSushiTypes;
    private List<int> selectedSushiTypes;
    private List<List<int>> cachedGuaranteedSushis;
    private RailData railData;

    private readonly HashSet<int> adPlateIndices = new HashSet<int>();
    private readonly HashSet<int> sushiMergePlateIndices = new HashSet<int>();
    private readonly HashSet<int> singleSlotPlateIndices = new HashSet<int>();
    private readonly HashSet<int> concentratedTypes = new HashSet<int>();
    private readonly HashSet<int> guaranteedPlateIndices = new HashSet<int>();
    private readonly HashSet<int> railPlateIndices = new HashSet<int>();
    private readonly HashSet<int> cantMergePlateIndices = new HashSet<int>();
    private readonly HashSet<(int, int)> guaranteedSlots = new HashSet<(int, int)>();
    private readonly Dictionary<int, int> eraseCountPerPlate = new Dictionary<int, int>();

    private List<int> layerSizePool = new List<int>();
    private int layerSizePoolIndex;
    #endregion

    #region Constructor
    public LevelGenerator(LevelData data)
    {
        levelData = data;
        SelectRandomSushiTypes();
        GenerateSushiPool();
        if (levelData.isRail) InitializeRail();
    }

    public RailData GetRailData() => railData;
    #endregion

    #region Initialization
    private void InitializeRail()
    {
        int rowCount = levelData.plateCount / 3;
        int railRow = Random.Range(0, rowCount);
        int rowStart = railRow * 3;
        railPlateIndices.Add(rowStart);
        railPlateIndices.Add(rowStart + 1);
        railPlateIndices.Add(rowStart + 2);

        int railSetCount = Mathf.Max(1, levelData.railSushiCount / 3);
        var railIds = new List<int>();

        // Build index-groups for allSushiTypes
        var typeIndexGroups = new Dictionary<int, List<int>>();
        for (int i = 0; i < allSushiTypes.Count; i++)
        {
            int t = allSushiTypes[i];
            if (!typeIndexGroups.ContainsKey(t)) typeIndexGroups[t] = new List<int>();
            typeIndexGroups[t].Add(i);
        }

        var available = new List<int>(typeIndexGroups.Keys);
        var indicesToRemove = new List<int>();
        Shuffle(available);

        foreach (int typeId in available)
        {
            if (railIds.Count >= railSetCount * 3) break;
            if (typeIndexGroups[typeId].Count < 3) continue;
            for (int i = 0; i < 3; i++) { railIds.Add(typeId); indicesToRemove.Add(typeIndexGroups[typeId][i]); }
        }

        indicesToRemove.Sort((a, b) => b.CompareTo(a));
        foreach (int idx in indicesToRemove) allSushiTypes.RemoveAt(idx);
        Shuffle(railIds);

        railData = new RailData { RowIndex = railRow, SushiTypeIds = railIds, RailPlateSprite = levelData.railPlateSprite };
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
        for (int i = 0; i < count; i++) concentratedTypes.Add(shuffled[i]);
    }

    private void GenerateSushiPool()
    {
        int totalCount = levelData.totalSushiSetCount * 3;
        int basePerType = (totalCount / levelData.sushiTypeCount / 3) * 3;
        allSushiTypes = new List<int>(totalCount);

        foreach (var typeId in selectedSushiTypes)
            for (int i = 0; i < basePerType; i++) allSushiTypes.Add(typeId);

        int remaining = totalCount - allSushiTypes.Count;
        int typeIndex = 0;
        while (remaining > 0)
        {
            int addCount = Mathf.Min(3, remaining);
            for (int i = 0; i < addCount; i++) allSushiTypes.Add(selectedSushiTypes[typeIndex]);
            remaining -= addCount;
            typeIndex = (typeIndex + 1) % selectedSushiTypes.Count;
        }
        Shuffle(allSushiTypes);
    }
    #endregion

    #region Layer Size Pool
    private void InitializeLayerSizePool(int totalLayers)
    {
        layerSizePool.Clear();
        int total = levelData.layerSize1Weight + levelData.layerSize2Weight + levelData.layerSize3Weight;
        if (total <= 0)
        {
            for (int i = 0; i < totalLayers; i++) layerSizePool.Add(Random.Range(1, 4));
            Shuffle(layerSizePool);
            return;
        }
        int count1 = Mathf.RoundToInt((float)levelData.layerSize1Weight / total * totalLayers);
        int count3 = Mathf.RoundToInt((float)levelData.layerSize3Weight / total * totalLayers);
        int count2 = totalLayers - count1 - count3;
        if (count2 < 0) { count3 += count2; count2 = 0; }
        for (int i = 0; i < count1; i++) layerSizePool.Add(1);
        for (int i = 0; i < count2; i++) layerSizePool.Add(2);
        for (int i = 0; i < count3; i++) layerSizePool.Add(3);
        Shuffle(layerSizePool);
    }

    private int GetWeightedLayerSize()
    {
        if (layerSizePoolIndex < layerSizePool.Count) return layerSizePool[layerSizePoolIndex++];
        int total = levelData.layerSize1Weight + levelData.layerSize2Weight + levelData.layerSize3Weight;
        if (total <= 0) return Random.Range(1, 4);
        int roll = Random.Range(0, total);
        if (roll < levelData.layerSize1Weight) return 1;
        if (roll < levelData.layerSize1Weight + levelData.layerSize2Weight) return 2;
        return 3;
    }
    #endregion

    #region Main Entry
    public List<PlateData> GeneratePlates()
    {
        cachedGuaranteedSushis = ExtractGuaranteedSushis();
        DetermineLockedPlates();

        // 단일 슬롯 플레이트 제외한 일반 플레이트 수만 사용
        int normalPlateCount = GetEffectivePlateCount() - singleSlotPlateIndices.Count;
        InitializeLayerSizePool(normalPlateCount * levelData.maxLayersPerPlate);
        layerSizePoolIndex = 0;

        var plates = new List<PlateData>();
        if (concentratedTypes.Count > 0) GeneratePlatesWithConcentration(plates);
        else GeneratePlatesNormal(plates);

        AssignLockedPlates(plates);
        if (levelData.hiddenReserveCount > 0) ApplyHiddenReserves(plates);
        FixTypeMultiples(plates);
        EnforceMinLayers(plates);
        ValidatePlates(plates);
        return plates;
    }

    private int GetEffectivePlateCount() => levelData.plateCount - railPlateIndices.Count;
    #endregion

    #region Erase & Active Count Helpers
    private void ComputeEraseCountPerPlate(int effectivePlateCount)
    {
        eraseCountPerPlate.Clear();
        if (levelData.sushiInitEraseCount <= 0) return;

        var erasable = new List<int>();
        for (int i = 0; i < effectivePlateCount; i++)
            if (!adPlateIndices.Contains(i) && !sushiMergePlateIndices.Contains(i) && !singleSlotPlateIndices.Contains(i))
                erasable.Add(i);
        Shuffle(erasable);

        int remaining = levelData.sushiInitEraseCount;
        int idx = 0;
        while (remaining > 0 && erasable.Count > 0)
        {
            int target = erasable[idx % erasable.Count];
            eraseCountPerPlate.TryGetValue(target, out int cur);
            if (cur < 2) { eraseCountPerPlate[target] = cur + 1; remaining--; }
            if (++idx >= erasable.Count * 2) break;
        }
    }

    // Used only in GeneratePlatesNormal path (eraseCount applied to all including lockedSushi)
    private int GetTargetActiveCount(int plateIndex)
    {
        bool isLockedSushi = sushiMergePlateIndices.Contains(plateIndex);
        bool isSingleSlot = singleSlotPlateIndices.Contains(plateIndex);
        int targetCount = isLockedSushi ? Random.Range(1, 4) : (isSingleSlot ? 1 : 3);
        if (eraseCountPerPlate.TryGetValue(plateIndex, out int ec))
            targetCount = Mathf.Max(1, targetCount - ec);
        return targetCount;
    }

    // Swap allSushiTypes[index] with a later element of different type to avoid triple
    private bool TrySwapToAvoidTriple(int index, int typeId)
    {
        for (int k = index + 1; k < allSushiTypes.Count; k++)
        {
            if (allSushiTypes[k] != typeId)
            {
                (allSushiTypes[index], allSushiTypes[k]) = (allSushiTypes[k], allSushiTypes[index]);
                return true;
            }
        }
        return false;
    }

    // Fill active slots up to targetCount, preventing same-triple in active
    private void FillActiveSlots(PlateData plate, ref int index, int targetCount)
    {
        while (plate.ActiveTypes.Count < targetCount && index < allSushiTypes.Count)
        {
            if (plate.ActiveTypes.Count == 2 &&
                plate.ActiveTypes[0] == plate.ActiveTypes[1] &&
                allSushiTypes[index] == plate.ActiveTypes[0] &&
                !TrySwapToAvoidTriple(index, plate.ActiveTypes[0]))
                break;
            plate.ActiveTypes.Add(allSushiTypes[index++]);
        }
    }
    #endregion

    #region Plate Generation
    private void GeneratePlatesNormal(List<PlateData> plates)
    {
        int effectivePlateCount = GetEffectivePlateCount();
        int index = 0;
        ComputeEraseCountPerPlate(effectivePlateCount);

        for (int i = 0; i < effectivePlateCount; i++)
        {
            var plateData = new PlateData();
            if (!adPlateIndices.Contains(i))
            {
                int targetCount = GetTargetActiveCount(i);
                if (i < cachedGuaranteedSushis.Count)
                {
                    plateData.ActiveTypes = cachedGuaranteedSushis[i];
                    for (int s = 0; s < plateData.ActiveTypes.Count; s++) guaranteedSlots.Add((i, s));
                    FillActiveSlots(plateData, ref index, targetCount);
                    guaranteedPlateIndices.Add(i);
                }
                else
                {
                    FillActiveSlots(plateData, ref index, targetCount);
                }
            }
            plates.Add(plateData);
        }

        var validIndices = GetValidPlateIndices(plates);
        DistributeLayersByTypeGroup(plates, validIndices, allSushiTypes.Skip(index).ToList());
        EnsureNoEmptyPlates(plates, new List<int>());
    }

    private void GeneratePlatesWithConcentration(List<PlateData> plates)
    {
        int effectivePlateCount = GetEffectivePlateCount();
        ComputeEraseCountPerPlate(effectivePlateCount);

        var concentratedSushis = new Dictionary<int, List<int>>();
        var dispersedSushis = new List<int>();

        foreach (var typeId in allSushiTypes)
        {
            if (concentratedTypes.Contains(typeId))
            {
                if (!concentratedSushis.ContainsKey(typeId)) concentratedSushis[typeId] = new List<int>();
                concentratedSushis[typeId].Add(typeId);
            }
            else dispersedSushis.Add(typeId);
        }

        for (int i = 0; i < effectivePlateCount; i++) plates.Add(new PlateData());

        int currentPlateIndex = 0;
        var activeTypeCount = new Dictionary<int, int>();

        foreach (var plateTypes in cachedGuaranteedSushis)
        {
            // adPlate이면 건너뜀
            while (currentPlateIndex < effectivePlateCount && adPlateIndices.Contains(currentPlateIndex))
                currentPlateIndex++;
            if (currentPlateIndex >= effectivePlateCount) break;

            plates[currentPlateIndex].ActiveTypes = new List<int>(plateTypes);
            for (int s = 0; s < plateTypes.Count; s++) guaranteedSlots.Add((currentPlateIndex, s));
            foreach (var typeId in plateTypes)
            {
                activeTypeCount.TryGetValue(typeId, out int c);
                activeTypeCount[typeId] = c + 1;
            }
            guaranteedPlateIndices.Add(currentPlateIndex++);
        }

        var concentratedShuffled = concentratedTypes.ToList();
        Shuffle(concentratedShuffled);
        var pendingLayerSushis = new List<int>();

        foreach (var typeId in concentratedShuffled)
        {
            if (!concentratedSushis.TryGetValue(typeId, out var typeSushis) || typeSushis.Count == 0) continue;
            activeTypeCount.TryGetValue(typeId, out int atc);

            int targetPlateStart = currentPlateIndex % effectivePlateCount;
            for (int i = 0; i < typeSushis.Count; i++)
            {
                int targetPlate = (targetPlateStart + (i / 2)) % effectivePlateCount;
                int attempts = 0;
                while (adPlateIndices.Contains(targetPlate) && attempts++ < effectivePlateCount)
                    targetPlate = (targetPlate + 1) % effectivePlateCount;

                if (attempts >= effectivePlateCount) { pendingLayerSushis.Add(typeSushis[i]); continue; }

                var plate = plates[targetPlate];
                bool isSingleSlot = singleSlotPlateIndices.Contains(targetPlate);
                int maxActive = isSingleSlot ? 1 : 3;
                eraseCountPerPlate.TryGetValue(targetPlate, out int ec);
                int effectiveMax = Mathf.Max(1, maxActive - ec);

                if (plate.ActiveTypes.Count < effectiveMax && atc < maxActive)
                {
                    plate.ActiveTypes.Add(typeSushis[i]);
                    activeTypeCount[typeId] = ++atc;
                    if (plate.ActiveTypes.Count == 3 && HasSameThree(plate.ActiveTypes))
                    {
                        if (i + 1 < typeSushis.Count)
                            (plate.ActiveTypes[2], typeSushis[i + 1]) = (typeSushis[i + 1], plate.ActiveTypes[2]);
                        else if (dispersedSushis.Count > 0)
                            (plate.ActiveTypes[2], dispersedSushis[0]) = (dispersedSushis[0], plate.ActiveTypes[2]);
                    }
                }
                else pendingLayerSushis.Add(typeSushis[i]);
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
            eraseCountPerPlate.TryGetValue(i, out int eraseCount);
            int targetCount = isLockedSushi ? Random.Range(1, maxActive + 1) : Mathf.Max(1, maxActive - eraseCount);

            while (plates[i].ActiveTypes.Count < targetCount && dispersedIndex < dispersedSushis.Count)
            {
                int typeToAdd = dispersedSushis[dispersedIndex];
                activeTypeCount.TryGetValue(typeToAdd, out int cnt);
                if (cnt < 3) { plates[i].ActiveTypes.Add(typeToAdd); activeTypeCount[typeToAdd] = cnt + 1; }
                else pendingLayerSushis.Add(typeToAdd);
                dispersedIndex++;
            }
            if (plates[i].ActiveTypes.Count == 3 && HasSameThree(plates[i].ActiveTypes))
                FixSameThreeInPlate(plates[i], dispersedSushis, ref dispersedIndex);
        }

        while (dispersedIndex < dispersedSushis.Count) pendingLayerSushis.Add(dispersedSushis[dispersedIndex++]);

        // Concentrated types in front of pending pool (higher layer priority)
        var concPending = new List<int>();
        var dispPending = new List<int>();
        foreach (var t in pendingLayerSushis)
        {
            if (concentratedTypes.Contains(t)) concPending.Add(t);
            else dispPending.Add(t);
        }
        Shuffle(concPending); Shuffle(dispPending);
        pendingLayerSushis = concPending;
        pendingLayerSushis.AddRange(dispPending);

        var validIndices = GetValidPlateIndices(plates);
        DistributeLayersByTypeGroup(plates, validIndices, pendingLayerSushis);
        EnsureNoEmptyPlates(plates, new List<int>());
    }
    #endregion

    #region Layer Distribution
    private void DistributeLayersByTypeGroup(List<PlateData> plates, List<int> validPlateIndices, List<int> pool)
    {
        if (pool.Count == 0) return;

        var typeGroups = new Dictionary<int, List<int>>();
        foreach (var t in pool)
        {
            if (!typeGroups.ContainsKey(t)) typeGroups[t] = new List<int>();
            typeGroups[t].Add(t);
        }

        var concTypes = typeGroups.Keys.Where(t => concentratedTypes.Contains(t)).ToList();
        var normTypes = typeGroups.Keys.Where(t => !concentratedTypes.Contains(t)).ToList();
        Shuffle(concTypes); Shuffle(normTypes);
        var orderedTypes = concTypes.Concat(normTypes).ToList();

        int maxLayerDepth = Mathf.Max(0, levelData.maxLayersPerPlate - 1);
        var depthBuckets = new SortedDictionary<int, List<int>>();
        var typeLastDepth = new Dictionary<int, int>();

        foreach (var typeId in orderedTypes)
        {
            var items = typeGroups[typeId];
            for (int setStart = 0; setStart < items.Count; setStart += 3)
            {
                typeLastDepth.TryGetValue(typeId, out int lastDepth);
                int minDepth = setStart == 0 ? 0 : Mathf.Min(lastDepth + 1, maxLayerDepth);
                int startDepth = maxLayerDepth > minDepth ? Random.Range(minDepth, maxLayerDepth + 1) : minDepth;
                typeLastDepth[typeId] = startDepth;

                if (!depthBuckets.ContainsKey(startDepth)) depthBuckets[startDepth] = new List<int>();
                int setEnd = Mathf.Min(setStart + 3, items.Count);
                for (int i = setStart; i < setEnd; i++)
                    depthBuckets[startDepth].Add(items[i]);
            }
        }

        var orderedPool = new List<int>(pool.Count);
        foreach (var kv in depthBuckets)
        {
            var group = new List<int>(kv.Value);
            Shuffle(group);
            orderedPool.AddRange(group);
        }

        int poolIndex = 0;
        var shuffledPlates = new List<int>(validPlateIndices);

        // minLayersPerPlate 우선 보장
        for (int pass = 0; pass < levelData.minLayersPerPlate; pass++)
        {
            foreach (int pi in shuffledPlates)
            {
                if (poolIndex >= orderedPool.Count) break;
                bool isSingle = singleSlotPlateIndices.Contains(pi);
                var layer = BuildLayer(orderedPool, ref poolIndex, plates[pi], isSingle, pi);
                if (layer != null && layer.Count > 0)
                    plates[pi].Layers.Add(new Layer(layer));
            }
        }

        // 나머지 maxLayersPerPlate까지 분배
        while (poolIndex < orderedPool.Count)
        {
            Shuffle(shuffledPlates);
            bool anyAdded = false;
            foreach (int pi in shuffledPlates)
            {
                if (poolIndex >= orderedPool.Count) break;
                if (plates[pi].Layers.Count >= levelData.maxLayersPerPlate) continue;
                bool isSingle = singleSlotPlateIndices.Contains(pi);
                var layer = BuildLayer(orderedPool, ref poolIndex, plates[pi], isSingle, pi);
                if (layer == null || layer.Count == 0) continue;
                plates[pi].Layers.Add(new Layer(layer));
                anyAdded = true;
            }
            if (!anyAdded) break;
        }
    }
    private bool TryAddToExistingSlot(int pi, int depth, int typeId,
    Dictionary<(int, int), List<int>> layerSlots,
    Dictionary<(int, int), int> slotMaxSizes)
    {
        var key = (pi, depth);
        if (!layerSlots.ContainsKey(key)) return false;
        var items = layerSlots[key];
        int maxSize = slotMaxSizes[key];
        int cnt = 0;
        foreach (var t in items) if (t == typeId) cnt++;
        if (cnt >= 2) return false;
        if (items.Count >= maxSize) return false;
        items.Add(typeId);
        return true;
    }

    private bool TryPlaceInSlot(
        int pi, int depth, int typeId,
        Dictionary<(int, int), List<int>> layerSlots,
        Dictionary<(int, int), int> slotMaxSizes,
        Dictionary<int, int> plateLayerCount,
        bool ignoreMaxLayer)
    {
        var key = (pi, depth);
        bool isSingleSlot = singleSlotPlateIndices.Contains(pi);

        if (!layerSlots.ContainsKey(key))
        {
            if (!ignoreMaxLayer)
            {
                plateLayerCount.TryGetValue(pi, out int cur);
                if (cur >= levelData.maxLayersPerPlate) return false;
            }
            layerSlots[key] = new List<int>();
            slotMaxSizes[key] = isSingleSlot ? 1 : GetWeightedLayerSize();
        }

        var layerTypes = layerSlots[key];
        int maxSize = slotMaxSizes[key];

        // Count same type already in this layer
        int countInLayer = 0;
        foreach (var t in layerTypes) if (t == typeId) countInLayer++;
        if (countInLayer >= 2) return false;
        if (layerTypes.Count >= maxSize) return false;

        bool wasEmpty = layerTypes.Count == 0;
        layerTypes.Add(typeId);
        if (wasEmpty)
        {
            plateLayerCount.TryGetValue(pi, out int c);
            plateLayerCount[pi] = c + 1;
        }
        return true;
    }

    private void ApplyLayerSlots(List<PlateData> plates, List<int> validPlateIndices,
        Dictionary<(int, int), List<int>> layerSlots)
    {
        var validSet = new HashSet<int>(validPlateIndices);
        foreach (var pi in validPlateIndices) plates[pi].Layers.Clear();

        // Collect and sort keys: by plateIndex then depth
        var keys = new List<(int, int)>(layerSlots.Count);
        foreach (var kv in layerSlots)
            if (validSet.Contains(kv.Key.Item1) && kv.Value.Count > 0)
                keys.Add(kv.Key);
        keys.Sort((a, b) => a.Item1 != b.Item1 ? a.Item1.CompareTo(b.Item1) : a.Item2.CompareTo(b.Item2));

        foreach (var key in keys)
        {
            int pi = key.Item1, depth = key.Item2;
            while (plates[pi].Layers.Count <= depth)
                plates[pi].Layers.Add(new Layer(new List<int>()));
            plates[pi].Layers[depth] = new Layer(new List<int>(layerSlots[key]));
        }

        foreach (var pi in validPlateIndices)
            plates[pi].Layers.RemoveAll(l => l.SushiTypes.Count == 0);
    }
    #endregion

    #region Post-Generation Passes
    private void EnsureNoEmptyPlates(List<PlateData> plates, List<int> pendingLayerSushis)
    {
        for (int i = 0; i < plates.Count; i++)
        {
            if (adPlateIndices.Contains(i)) continue;

            var plate = plates[i];
            bool isSingleSlot = singleSlotPlateIndices.Contains(i);
            int maxActive = isSingleSlot ? 1 : 3;
            eraseCountPerPlate.TryGetValue(i, out int eraseCount);
            int effectiveMax = Mathf.Max(1, maxActive - eraseCount);

            if (plate.ActiveTypes.Count == 0)
            {
                if (plate.Layers.Count > 0)
                {
                    var firstLayer = plate.Layers[0];
                    int moveCount = Mathf.Min(effectiveMax, firstLayer.SushiTypes.Count);
                    for (int k = 0; k < moveCount; k++) plate.ActiveTypes.Add(firstLayer.SushiTypes[k]);
                    firstLayer.SushiTypes.RemoveRange(0, moveCount);
                    if (firstLayer.SushiTypes.Count == 0) plate.Layers.RemoveAt(0);
                }
                else if (pendingLayerSushis.Count > 0)
                {
                    int minCount = Mathf.Min(effectiveMax, pendingLayerSushis.Count);
                    for (int j = 0; j < minCount; j++) { plate.ActiveTypes.Add(pendingLayerSushis[0]); pendingLayerSushis.RemoveAt(0); }
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
                            for (int k = 0; k < moveCount; k++) plate.ActiveTypes.Add(layerToMove.SushiTypes[k]);
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
                for (int j = 0; j < minCount; j++) { plate.ActiveTypes.Add(pendingLayerSushis[0]); pendingLayerSushis.RemoveAt(0); }
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
        foreach (var plate in plates)
        {
            foreach (var t in plate.ActiveTypes) { typeCountMap.TryGetValue(t, out int c); typeCountMap[t] = c + 1; }
            foreach (var layer in plate.Layers)
                foreach (var t in layer.SushiTypes) { typeCountMap.TryGetValue(t, out int c); typeCountMap[t] = c + 1; }
        }
        if (railData != null)
            foreach (var t in railData.SushiTypeIds) { typeCountMap.TryGetValue(t, out int c); typeCountMap[t] = c + 1; }

        foreach (var kvp in typeCountMap)
        {
            int remainder = kvp.Value % 3;
            if (remainder == 0) continue;
            if (!selectedSushiTypes.Contains(kvp.Key))
            { Debug.LogError($"[LevelGenerator] FixTypeMultiples - 선택되지 않은 타입 {kvp.Key}"); continue; }

            int toAdd = 3 - remainder;
            for (int added = 0; added < toAdd; added++)
            {
                bool merged = false;
                // 기존 레이어에 병합 우선 (size1 레이어 추가 방지)
                for (int i = 0; i < plates.Count && !merged; i++)
                {
                    if (adPlateIndices.Contains(i)) continue;
                    for (int li = 0; li < plates[i].Layers.Count && !merged; li++)
                    {
                        var layer = plates[i].Layers[li];
                        int cntInLayer = 0;
                        foreach (var t in layer.SushiTypes) if (t == kvp.Key) cntInLayer++;
                        if (layer.SushiTypes.Count < 3 && cntInLayer < 2)
                        {
                            layer.AddSushi(kvp.Key);
                            merged = true;
                        }
                    }
                }
                // Fallback: 새 레이어 추가
                if (!merged)
                    for (int i = 0; i < plates.Count; i++)
                        if (!adPlateIndices.Contains(i))
                        { plates[i].Layers.Add(new Layer(new List<int> { kvp.Key })); break; }
            }
        }
    }

    private void ApplyHiddenReserves(List<PlateData> plates)
    {
        var candidates = new List<(PlateData, int, int)>();
        foreach (var plate in plates)
        {
            if (plate.State == PlateState.LockedAd) continue;
            for (int li = 0; li < plate.Layers.Count; li++)
                for (int si = 0; si < plate.Layers[li].SushiTypes.Count; si++)
                    candidates.Add((plate, li, si));
        }
        if (candidates.Count == 0) { Debug.LogWarning("[LevelGenerator] Hidden Reserve 적용 불가 - 예비 초밥 없음"); return; }
        Shuffle(candidates);
        int count = Mathf.Min(levelData.hiddenReserveCount, candidates.Count);
        for (int i = 0; i < count; i++)
        {
            var (plate, li, si) = candidates[i];
            plate.Layers[li].SetHiddenState(si, true);
        }
    }
    #endregion

    #region Locked Plates
    private void DetermineLockedPlates()
    {
        adPlateIndices.Clear(); sushiMergePlateIndices.Clear();
        singleSlotPlateIndices.Clear(); cantMergePlateIndices.Clear();

        int effectivePlateCount = GetEffectivePlateCount();
        int guaranteedPlateCount = cachedGuaranteedSushis.Count;

        // locked 후보: 전체 플레이트 (guaranteed 포함)
        var lockedCandidates = new List<int>();
        for (int i = 0; i < effectivePlateCount; i++) lockedCandidates.Add(i);
        Shuffle(lockedCandidates);

        int mergeUnlockCount = Mathf.Min(levelData.mergeUnlockCount, levelData.lockedPlateCount);
        int totalLockedCount = Mathf.Min(levelData.lockedPlateCount, lockedCandidates.Count);

        if (totalLockedCount < levelData.lockedPlateCount)
            Debug.LogError($"[LevelGenerator] lockedPlateCount({levelData.lockedPlateCount}) 충족 불가 - 가능: {lockedCandidates.Count}");

        for (int i = 0; i < mergeUnlockCount && i < totalLockedCount; i++)
            sushiMergePlateIndices.Add(lockedCandidates[i]);
        for (int i = mergeUnlockCount; i < totalLockedCount; i++)
            adPlateIndices.Add(lockedCandidates[i]);

        // singleSlot/cantMerge 후보: guaranteed 플레이트 제외 (slotCount=1 충돌 방지)
        var remaining = new List<int>();
        for (int i = guaranteedPlateCount; i < effectivePlateCount; i++)
            if (!adPlateIndices.Contains(i) && !sushiMergePlateIndices.Contains(i))
                remaining.Add(i);

        int singleSlotCount = Mathf.Min(levelData.singleSlotPlateCount, remaining.Count);
        for (int i = 0; i < singleSlotCount; i++) singleSlotPlateIndices.Add(remaining[i]);

        var cantMergeCandidates = new List<int>();
        for (int i = 0; i < effectivePlateCount; i++)
            if (!adPlateIndices.Contains(i) && !sushiMergePlateIndices.Contains(i) && !singleSlotPlateIndices.Contains(i))
                cantMergeCandidates.Add(i);
        Shuffle(cantMergeCandidates);

        int cantMergeCount = Mathf.Min(levelData.cantMergePlateCount, cantMergeCandidates.Count);
        if (cantMergeCount < levelData.cantMergePlateCount)
            Debug.LogError($"[LevelGenerator] cantMergePlateCount({levelData.cantMergePlateCount}) 충족 불가 - 가능: {cantMergeCandidates.Count}");
        for (int i = 0; i < cantMergeCount; i++) cantMergePlateIndices.Add(cantMergeCandidates[i]);
    }

    private void AssignLockedPlates(List<PlateData> plates)
    {
        var availableSushiTypes = GetAvailableSushiTypes(plates);
        Shuffle(availableSushiTypes);

        int typeIndex = 0;
        foreach (var plateIndex in sushiMergePlateIndices)
        {
            if (typeIndex >= availableSushiTypes.Count) { Debug.LogWarning("[LevelGenerator] LockedSushi 타입 부족"); break; }
            plates[plateIndex].State = PlateState.LockedSushi;
            plates[plateIndex].RequiredSushiTypeId = availableSushiTypes[typeIndex++];
        }
        foreach (var plateIndex in adPlateIndices) plates[plateIndex].State = PlateState.LockedAd;
        foreach (var plateIndex in singleSlotPlateIndices) plates[plateIndex].SlotCount = 1;
        foreach (var plateIndex in cantMergePlateIndices) plates[plateIndex].State = PlateState.CantMerge;

        AssignLockedSushis(plates);
        ResolveLockedPlateDeadlocks(plates);
        ResolveInterLockedDeadlocks(plates); // 교차 데드락 해소
    }
    private void ResolveInterLockedDeadlocks(List<PlateData> plates)
    {
        foreach (var plateIndexA in sushiMergePlateIndices)
        {
            int requiredType = plates[plateIndexA].RequiredSushiTypeId;
            if (requiredType < 0) continue;

            foreach (var plateIndexB in sushiMergePlateIndices)
            {
                if (plateIndexA == plateIndexB) continue;

                var plateB = plates[plateIndexB];

                // active에서 제거
                for (int i = plateB.ActiveTypes.Count - 1; i >= 0; i--)
                {
                    if (plateB.ActiveTypes[i] != requiredType) continue;
                    bool moved = TryMoveToOtherPlate(plates, plateIndexB,
                        new Layer(new List<int> { requiredType }), out _);
                    if (moved) plateB.ActiveTypes.RemoveAt(i);
                }

                // reserve에서 제거
                for (int layerIdx = plateB.Layers.Count - 1; layerIdx >= 0; layerIdx--)
                {
                    var layer = plateB.Layers[layerIdx];
                    var indicesToMove = new List<int>();
                    for (int si = 0; si < layer.SushiTypes.Count; si++)
                        if (layer.SushiTypes[si] == requiredType) indicesToMove.Add(si);
                    if (indicesToMove.Count == 0) continue;

                    bool moved = TryMoveToOtherPlate(plates, plateIndexB,
                        new Layer(new List<int>(indicesToMove.Select(_ => requiredType))), out _);
                    if (moved)
                    {
                        foreach (var idx in indicesToMove.OrderByDescending(x => x))
                            layer.SushiTypes.RemoveAt(idx);
                        if (layer.SushiTypes.Count == 0)
                            plateB.Layers.RemoveAt(layerIdx);
                    }
                }
            }
        }
    }
    private void ResolveLockedPlateDeadlocks(List<PlateData> plates)
    {
        foreach (var plateIndex in sushiMergePlateIndices)
        {
            var plate = plates[plateIndex];
            int requiredType = plate.RequiredSushiTypeId;
            if (requiredType < 0) continue;

            // Move required type out of active slots
            for (int i = plate.ActiveTypes.Count - 1; i >= 0; i--)
            {
                if (plate.ActiveTypes[i] != requiredType) continue;
                bool moved = TryMoveToOtherPlate(plates, plateIndex, new Layer(new List<int> { requiredType }), out int otherIdx);
                if (moved) plate.ActiveTypes.RemoveAt(i);
            }

            // Move required type out of reserve layers
            for (int layerIdx = plate.Layers.Count - 1; layerIdx >= 0; layerIdx--)
            {
                var layer = plate.Layers[layerIdx];
                var indicesToMove = new List<int>();
                for (int si = 0; si < layer.SushiTypes.Count; si++)
                    if (layer.SushiTypes[si] == requiredType) indicesToMove.Add(si);
                if (indicesToMove.Count == 0) continue;

                bool moved = TryMoveToOtherPlate(plates, plateIndex, new Layer(new List<int>(indicesToMove.Select(_ => requiredType))), out _);
                if (moved)
                {
                    foreach (var idx in indicesToMove.OrderByDescending(x => x)) layer.SushiTypes.RemoveAt(idx);
                    if (layer.SushiTypes.Count == 0) plate.Layers.RemoveAt(layerIdx);
                }
            }
        }
    }

    private bool TryMoveToOtherPlate(List<PlateData> plates, int fromIndex, Layer layerToAdd, out int targetIdx)
    {
        for (int pass = 0; pass < 2; pass++)
        {
            for (int otherIdx = 0; otherIdx < plates.Count; otherIdx++)
            {
                if (otherIdx == fromIndex || adPlateIndices.Contains(otherIdx)) continue;
                if (sushiMergePlateIndices.Contains(otherIdx)) continue;
                if (pass == 0 && plates[otherIdx].Layers.Count >= levelData.maxLayersPerPlate) continue;
                plates[otherIdx].Layers.Add(layerToAdd);
                targetIdx = otherIdx;
                return true;
            }
        }
        targetIdx = -1;
        return false;
    }

    private void AssignLockedSushis(List<PlateData> plates)
    {
        if (levelData.lockedSushiCount <= 0) return;

        var activeSlots = new List<(int, int)>();
        var layerSlots = new List<(int, int, int)>();

        for (int i = 0; i < plates.Count; i++)
        {
            if (plates[i].State != PlateState.Normal) continue;
            for (int j = 0; j < plates[i].ActiveTypes.Count; j++) activeSlots.Add((i, j));
            for (int li = 0; li < plates[i].Layers.Count; li++)
                for (int j = 0; j < plates[i].Layers[li].Count; j++)
                    layerSlots.Add((i, j, li));
        }
        Shuffle(activeSlots); Shuffle(layerSlots);

        int lockedCount = levelData.lockedSushiCount;
        if (activeSlots.Count > 0)
        {
            var slot = activeSlots[0];
            plates[slot.Item1].ActiveLockStages[slot.Item2] = 3;
            lockedCount--;
        }
        int assigned = 0;
        foreach (var slot in layerSlots)
        {
            if (assigned >= lockedCount) break;
            plates[slot.Item1].Layers[slot.Item3].SetLockStage(slot.Item2, 3);
            assigned++;
        }
    }
    #endregion

    #region Guaranteed Sushis & Validation
    private List<List<int>> ExtractGuaranteedSushis()
    {
        var result = new List<List<int>>();
        if (levelData.guaranteedMergeSets <= 0) return result;

        var typeCount = new Dictionary<int, int>();
        foreach (var t in allSushiTypes) { typeCount.TryGetValue(t, out int c); typeCount[t] = c + 1; }

        var availableTypes = selectedSushiTypes.Where(t => typeCount.TryGetValue(t, out int c) && c >= 3).ToList();
        Shuffle(availableTypes);

        int setsToCreate = Mathf.Min(levelData.guaranteedMergeSets, availableTypes.Count);
        var possibleDistributions = new List<List<int>> { new List<int> { 1, 2 }, new List<int> { 2, 1 }, new List<int> { 1, 1, 1 } };

        for (int set = 0; set < setsToCreate; set++)
        {
            int typeId = availableTypes[set];
            var typeIndices = new List<int>();
            for (int i = 0; i < allSushiTypes.Count; i++)
                if (allSushiTypes[i] == typeId) { typeIndices.Add(i); if (typeIndices.Count == 3) break; }

            foreach (var idx in typeIndices.OrderByDescending(i => i)) allSushiTypes.RemoveAt(idx);

            // 튜토리얼 스테이지는 무조건 {2, 1} 배치
            List<int> distribution;
            if (levelData.isTutorialStage && set == 0)
                distribution = new List<int> { 2, 1 };
            else
                distribution = possibleDistributions[Random.Range(0, possibleDistributions.Count)];

            foreach (var count in distribution)
            {
                var plateTypes = new List<int>(count);
                for (int i = 0; i < count; i++) plateTypes.Add(typeId);
                result.Add(plateTypes);
            }
        }

        // 튜토리얼 스테이지는 셔플 없이 순서 유지 (2개 plate가 앞에 오도록)
        if (!levelData.isTutorialStage)
            Shuffle(result);

        return result;
    }

    private void ValidatePlates(List<PlateData> plates)
    {
        int totalSushis = 0;
        var typeCount = new Dictionary<int, int>();
        int size1 = 0, size2 = 0, size3 = 0;

        foreach (var plate in plates)
        {
            foreach (var t in plate.ActiveTypes) { typeCount.TryGetValue(t, out int c); typeCount[t] = c + 1; totalSushis++; }
            foreach (var layer in plate.Layers)
            {
                // 단일 슬롯 플레이트는 size 비율 카운트 제외
                bool isSingleSlot = plate.SlotCount == 1;
                if (!isSingleSlot)
                {
                    int s = layer.SushiTypes.Count;
                    if (s == 1) size1++;
                    else if (s == 2) size2++;
                    else if (s >= 3) size3++;
                }
                foreach (var t in layer.SushiTypes) { typeCount.TryGetValue(t, out int c); typeCount[t] = c + 1; totalSushis++; }
            }
        }
        if (railData != null)
            foreach (var t in railData.SushiTypeIds) { typeCount.TryGetValue(t, out int c); typeCount[t] = c + 1; totalSushis++; }

        int totalLayers = size1 + size2 + size3;
        float r1 = totalLayers > 0 ? size1 * 100f / totalLayers : 0f;
        float r2 = totalLayers > 0 ? size2 * 100f / totalLayers : 0f;
        float r3 = totalLayers > 0 ? size3 * 100f / totalLayers : 0f;

        Debug.Log($"[LevelGenerator] 총 초밥: {totalSushis} | 3배수: {totalSushis % 3 == 0} | 타입 수: {typeCount.Count}/{levelData.sushiTypeCount}");
        Debug.Log($"[LevelGenerator] 레이어 총합: {totalLayers} | size1: {size1}({r1:F1}%) | size2: {size2}({r2:F1}%) | size3: {size3}({r3:F1}%)");
        Debug.Log($"[LevelGenerator] 설정 비율 — size1: {levelData.layerSize1Weight} | size2: {levelData.layerSize2Weight} | size3: {levelData.layerSize3Weight}");

        if (typeCount.Count != levelData.sushiTypeCount)
            Debug.LogError($"[LevelGenerator] 타입 수 불일치: 실제 {typeCount.Count} / 설정 {levelData.sushiTypeCount}");
        foreach (var kvp in typeCount)
            if (kvp.Value % 3 != 0) Debug.LogError($"[LevelGenerator] 타입 {kvp.Key}: {kvp.Value}개 - 3의 배수 아님");
        if (totalSushis % 3 != 0) Debug.LogError($"[LevelGenerator] 초밥 총합 3의 배수 아님 ({totalSushis})");
    }

    #endregion

    #region Utilities
    private List<int> GetValidPlateIndices(List<PlateData> plates)
    {
        var result = new List<int>(plates.Count);
        for (int i = 0; i < plates.Count; i++)
            if (!adPlateIndices.Contains(i)) result.Add(i);
        return result;
    }

    private List<int> GetAvailableSushiTypes(List<PlateData> plates)
    {
        var types = new HashSet<int>();
        foreach (var plate in plates)
        {
            foreach (var t in plate.ActiveTypes) types.Add(t);
            foreach (var layer in plate.Layers) foreach (var t in layer.SushiTypes) types.Add(t);
        }
        return new List<int>(types);
    }
    private int CountTypeInReserve(PlateData plate, int typeId, List<int> currentLayer)
    {
        int count = 0;
        foreach (var layer in plate.Layers)
            foreach (var t in layer.SushiTypes)
                if (t == typeId) count++;
        foreach (var t in currentLayer)
            if (t == typeId) count++;
        return count;
    }

    private bool TrySwapCandidate(List<int> pool, int poolIndex, PlateData plate, List<int> currentLayer, int plateIndex = -1)
    {
        int candidate = pool[poolIndex];

        // LockedSushi 플레이트면 requiredType 진입 차단
        if (plateIndex >= 0 && sushiMergePlateIndices.Contains(plateIndex) && candidate == plate.RequiredSushiTypeId)
        {
            for (int s = poolIndex + 1; s < pool.Count; s++)
            {
                if (pool[s] != plate.RequiredSushiTypeId)
                {
                    (pool[poolIndex], pool[s]) = (pool[s], pool[poolIndex]);
                    return true;
                }
            }
            return false;
        }

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

    private List<int> BuildLayer(List<int> pool, ref int poolIndex, PlateData plate, bool isSingleSlot, int plateIndex = -1)
    {
        if (poolIndex >= pool.Count) return null;

        int layerSize = isSingleSlot ? 1 : GetWeightedLayerSize();
        var layerTypes = new List<int>(layerSize);

        // 첫 번째 초밥 배치 시 LockedSushi 플레이트 requiredType 차단
        if (!TrySwapCandidate(pool, poolIndex, plate, layerTypes, plateIndex)) return null;
        layerTypes.Add(pool[poolIndex++]);

        for (int k = 1; k < layerSize && poolIndex < pool.Count; k++)
        {
            if (!TrySwapCandidate(pool, poolIndex, plate, layerTypes, plateIndex)) break;
            layerTypes.Add(pool[poolIndex++]);
        }

        return layerTypes;
    }
    private bool HasSameThree(List<int> types) => types.Count == 3 && types[0] == types[1] && types[1] == types[2];

    private void FixSameThreeInPlate(PlateData plate, List<int> dispersed, ref int idx)
    {
        if (!HasSameThree(plate.ActiveTypes)) return;
        for (int i = idx; i < dispersed.Count; i++)
        {
            if (dispersed[i] != plate.ActiveTypes[0])
            { (plate.ActiveTypes[2], dispersed[i]) = (dispersed[i], plate.ActiveTypes[2]); return; }
        }
    }

    private void FixSameThree(List<int> types, ref int currentIndex)
    {
        if (!HasSameThree(types)) return;
        int swapTarget = types[2];
        for (int i = currentIndex; i < allSushiTypes.Count; i++)
            if (allSushiTypes[i] != types[0]) { types[2] = allSushiTypes[i]; allSushiTypes[i] = swapTarget; return; }
        for (int i = 0; i < currentIndex; i++)
            if (allSushiTypes[i] != types[0]) { types[2] = allSushiTypes[i]; allSushiTypes[i] = swapTarget; return; }
        foreach (var typeId in selectedSushiTypes)
            if (typeId != types[0]) { types[2] = typeId; allSushiTypes.Add(types[0]); return; }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
    #endregion
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