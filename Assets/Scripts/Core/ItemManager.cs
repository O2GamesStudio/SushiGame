using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    [SerializeField] private PlateManager plateManager;
    [SerializeField] private Transform collectCenter;

    private bool isWaitingForTargetSelection = false;
    private System.Action<Sushi> onSushiSelected;
    private bool isProcessingItem = false;

    public bool IsWaitingForTarget => isWaitingForTargetSelection;

    private void Awake()
    {
        Instance = this;
    }

    public void UseRandomSetRemover()
    {
        if (isProcessingItem) return;

        var allActiveSushis = GetAllActiveSushis();
        if (allActiveSushis.Count == 0) return;

        var typeCountMap = new Dictionary<int, int>();

        foreach (var sushi in allActiveSushis)
        {
            if (!typeCountMap.ContainsKey(sushi.TypeId))
                typeCountMap[sushi.TypeId] = 0;
            typeCountMap[sushi.TypeId]++;
        }

        var allReserveTypes = GetAllReserveTypes();
        foreach (var typeId in allReserveTypes)
        {
            if (!typeCountMap.ContainsKey(typeId))
                typeCountMap[typeId] = 0;
            typeCountMap[typeId]++;
        }

        var validTypes = typeCountMap.Where(kvp => kvp.Value >= 3).Select(kvp => kvp.Key).ToList();

        if (validTypes.Count == 0)
        {
            return;
        }

        isProcessingItem = true;
        int targetType = validTypes[Random.Range(0, validTypes.Count)];
        RemoveSushiSet(targetType);
    }

    public void UseTimeFreezer()
    {
        GameManager.Instance?.FreezeTimer(10f);
    }

    public void UseSushiShuffler()
    {
        if (isProcessingItem) return;

        var allActiveSushis = GetAllActiveSushis();
        var allReserveTypes = GetAllReserveTypes();

        if (allActiveSushis.Count == 0 && allReserveTypes.Count == 0) return;

        var combinedTypes = new List<int>();
        combinedTypes.AddRange(allActiveSushis.Select(s => s.TypeId));
        combinedTypes.AddRange(allReserveTypes);

        Shuffle(combinedTypes);

        int index = 0;

        foreach (var sushi in allActiveSushis)
        {
            var data = SushiPool.Instance.GetData(combinedTypes[index]);
            if (data != null)
            {
                sushi.Initialize(combinedTypes[index], data.sprite);
            }
            index++;
        }

        foreach (var plate in plateManager.GetAllPlates())
        {
            if (!plate.gameObject.activeSelf) continue;
            if (plate.State == PlateState.LockedAd) continue;

            var layers = plate.GetAllLayers();
            foreach (var layer in layers)
            {
                for (int i = 0; i < layer.SushiTypes.Count; i++)
                {
                    if (index < combinedTypes.Count)
                    {
                        layer.SushiTypes[i] = combinedTypes[index++];
                    }
                }
            }
        }

        PreventSameThreeInPlates(allActiveSushis, combinedTypes);

        foreach (var plate in plateManager.GetAllPlates())
        {
            if (!plate.gameObject.activeSelf) continue;
            plate.UpdateReserveDisplay();
            plate.RecheckMerge();
        }
    }

    private void PreventSameThreeInPlates(List<Sushi> allActiveSushis, List<int> combinedTypes)
    {
        var plates = plateManager.GetAllPlates();

        foreach (var plate in plates)
        {
            if (!plate.gameObject.activeSelf || plate.IsLocked) continue;

            var plateSushis = plate.GetActiveSushis();
            if (plateSushis.Count != 3) continue;

            if (plateSushis[0].TypeId == plateSushis[1].TypeId &&
                plateSushis[1].TypeId == plateSushis[2].TypeId)
            {
                int sameType = plateSushis[0].TypeId;

                for (int i = 0; i < allActiveSushis.Count; i++)
                {
                    if (allActiveSushis[i].TypeId != sameType &&
                        !BelongsToSamePlate(allActiveSushis[i], plate))
                    {
                        int targetType = allActiveSushis[i].TypeId;

                        var data1 = SushiPool.Instance.GetData(targetType);
                        var data2 = SushiPool.Instance.GetData(sameType);

                        if (data1 != null && data2 != null)
                        {
                            plateSushis[2].Initialize(targetType, data1.sprite);
                            allActiveSushis[i].Initialize(sameType, data2.sprite);
                            break;
                        }
                    }
                }
            }
        }
    }

    private bool BelongsToSamePlate(Sushi sushi, Plate targetPlate)
    {
        return sushi.CurrentPlate == targetPlate;
    }

    public void UseTargetSetRemover()
    {
        if (isProcessingItem) return;

        isWaitingForTargetSelection = true;
        onSushiSelected = (selectedSushi) =>
        {
            isWaitingForTargetSelection = false;
            onSushiSelected = null;
            isProcessingItem = true;
            RemoveSushiSet(selectedSushi.TypeId);
        };
    }

    public void OnSushiClicked(Sushi sushi)
    {
        if (isWaitingForTargetSelection && onSushiSelected != null)
        {
            onSushiSelected.Invoke(sushi);
        }
    }

    private void RemoveSushiSet(int targetType)
    {
        var sushisToRemove = new List<Sushi>();
        var platesToCheck = new HashSet<Plate>();

        var allActiveSushis = GetAllActiveSushis();
        var sameSushis = allActiveSushis
            .Where(s => s.TypeId == targetType && s.CurrentPlate != null)
            .ToList();

        Shuffle(sameSushis);

        int needed = 3;
        for (int i = 0; i < sameSushis.Count && needed > 0; i++)
        {
            sushisToRemove.Add(sameSushis[i]);
            platesToCheck.Add(sameSushis[i].CurrentPlate);
            needed--;
        }

        List<(int typeId, Plate plate)> reserveRemoved = new List<(int, Plate)>();

        if (needed > 0)
        {
            reserveRemoved = RemoveTypesFromReserve(targetType, needed);
        }

        foreach (var sushi in sushisToRemove)
        {
            if (sushi.CurrentPlate != null)
            {
                sushi.CurrentPlate.RemoveSpecificSushi(sushi, true, true);
            }
        }

        AnimateAndRemoveSushis(sushisToRemove, reserveRemoved, platesToCheck);
    }

    private List<(int typeId, Plate plate)> RemoveTypesFromReserve(int targetType, int count)
    {
        var removed = new List<(int typeId, Plate plate)>();

        foreach (var plate in plateManager.GetAllPlates())
        {
            if (!plate.gameObject.activeSelf || plate.State == PlateState.LockedAd) continue;
            if (removed.Count >= count) break;

            var layers = plate.GetAllLayers();
            var layersToRemove = new List<int>();

            for (int layerIdx = 0; layerIdx < layers.Count; layerIdx++)
            {
                if (removed.Count >= count) break;

                var layer = layers[layerIdx];
                var lockStages = layer.GetLockStages();
                var indicesToRemove = new List<int>();

                for (int i = layer.SushiTypes.Count - 1; i >= 0 && removed.Count < count; i--)
                {
                    if (layer.SushiTypes[i] == targetType)
                    {
                        removed.Add((targetType, plate));
                        indicesToRemove.Add(i);
                    }
                }

                foreach (var idx in indicesToRemove.OrderByDescending(x => x))
                {
                    layer.SushiTypes.RemoveAt(idx);

                    if (layer.SlotIndices.Count > idx)
                    {
                        layer.SlotIndices.RemoveAt(idx);
                    }

                    if (lockStages != null && lockStages.Count > idx)
                    {
                        lockStages.RemoveAt(idx);
                    }
                }

                if (layer.SushiTypes.Count == 0)
                {
                    layersToRemove.Add(layerIdx);
                }
            }

            foreach (var layerIdx in layersToRemove.OrderByDescending(x => x))
            {
                plate.RemoveLayer(layerIdx);
            }

            if (layersToRemove.Count > 0)
            {
                plate.UpdateReserveDisplay();
            }
        }

        return removed;
    }

    private void AnimateAndRemoveSushis(List<Sushi> activeSushis, List<(int typeId, Plate plate)> reserveTypes, HashSet<Plate> platesToCheck)
    {
        Vector3 center = collectCenter != null ? collectCenter.position : Vector3.zero;
        int totalCount = activeSushis.Count + reserveTypes.Count;
        int completedCount = 0;

        foreach (var sushi in activeSushis)
        {
            sushi.transform.DOMove(center, 0.5f).SetEase(Ease.InBack);
            sushi.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    SushiLockSystem.Instance?.ClearLockedSushi(sushi);
                    SushiPool.Instance.Return(sushi);

                    completedCount++;
                    if (completedCount >= totalCount)
                    {
                        OnItemAnimationComplete(platesToCheck);
                    }
                });
        }

        foreach (var (typeId, plate) in reserveTypes)
        {
            var tempSushi = SushiPool.Instance.Get(typeId);

            Vector3 startPos = plate.transform.position + Vector3.down * 0.5f;
            tempSushi.transform.position = startPos;
            tempSushi.transform.localScale = Vector3.one * 0.7f;

            tempSushi.transform.DOMove(center, 0.5f).SetEase(Ease.InBack);
            tempSushi.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    SushiPool.Instance.Return(tempSushi);

                    completedCount++;
                    if (completedCount >= totalCount)
                    {
                        OnItemAnimationComplete(platesToCheck);
                    }
                });
        }
    }

    private void OnItemAnimationComplete(HashSet<Plate> platesToCheck)
    {
        foreach (var plate in platesToCheck)
        {
            if (plate != null && plate.gameObject.activeSelf)
            {
                plate.CheckAndRefill();
                plate.RecheckMerge();
            }
        }

        isProcessingItem = false;
        GameStateChecker.Instance?.CheckWinCondition();
    }

    private List<Sushi> GetAllActiveSushis()
    {
        var result = new List<Sushi>();
        foreach (var plate in plateManager.GetAllPlates())
        {
            if (!plate.gameObject.activeSelf) continue;
            if (plate.State == PlateState.LockedAd) continue;

            result.AddRange(plate.GetActiveSushis());
        }
        return result;
    }

    private List<int> GetAllReserveTypes()
    {
        var result = new List<int>();
        foreach (var plate in plateManager.GetAllPlates())
        {
            if (!plate.gameObject.activeSelf) continue;
            if (plate.State == PlateState.LockedAd) continue;

            var layers = plate.GetAllLayers();
            foreach (var layer in layers)
            {
                result.AddRange(layer.SushiTypes);
            }
        }
        return result;
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