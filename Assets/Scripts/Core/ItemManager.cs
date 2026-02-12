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

    public bool IsWaitingForTarget => isWaitingForTargetSelection;

    private void Awake()
    {
        Instance = this;
    }

    public void UseRandomSetRemover()
    {
        var allActiveSushis = GetAllActiveSushis();
        if (allActiveSushis.Count == 0) return;

        var randomSushi = allActiveSushis[Random.Range(0, allActiveSushis.Count)];
        int targetType = randomSushi.TypeId;

        RemoveSushiSet(targetType, randomSushi);
    }

    public void UseTimeFreezer()
    {
        GameManager.Instance?.FreezeTimer(10f);
    }

    public void UseSushiShuffler()
    {
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
        isWaitingForTargetSelection = true;
        onSushiSelected = (selectedSushi) =>
        {
            RemoveSushiSet(selectedSushi.TypeId, selectedSushi);
            isWaitingForTargetSelection = false;
            onSushiSelected = null;
        };
    }

    public void OnSushiClicked(Sushi sushi)
    {
        if (isWaitingForTargetSelection && onSushiSelected != null)
        {
            onSushiSelected.Invoke(sushi);
        }
    }

    private void RemoveSushiSet(int targetType, Sushi guaranteedSushi)
    {
        var sushisToRemove = new List<Sushi>();
        var platesToCheck = new HashSet<Plate>();

        if (guaranteedSushi != null && guaranteedSushi.CurrentPlate != null)
        {
            sushisToRemove.Add(guaranteedSushi);
            platesToCheck.Add(guaranteedSushi.CurrentPlate);
        }

        var allActiveSushis = GetAllActiveSushis();
        var sameSushis = allActiveSushis
            .Where(s => s.TypeId == targetType && s != guaranteedSushi && s.CurrentPlate != null)
            .ToList();

        Shuffle(sameSushis);

        int needed = 2;
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
            needed -= reserveRemoved.Count;
        }

        if (sushisToRemove.Count + reserveRemoved.Count < 3)
        {
            Debug.LogWarning($"[ItemManager] 타입 {targetType}의 초밥이 총 3개 미만입니다.");
            return;
        }

        foreach (var sushi in sushisToRemove)
        {
            if (sushi.CurrentPlate != null)
            {
                sushi.CurrentPlate.RemoveSpecificSushi(sushi);
            }
        }

        AnimateAndRemoveSushis(sushisToRemove, reserveRemoved);

        foreach (var plate in platesToCheck)
        {
            if (plate != null && plate.gameObject.activeSelf)
            {
                if (plate.ActiveCount == 0 && plate.LayerCount > 0)
                {
                    plate.RecheckMerge();
                }
            }
        }
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

    private void AnimateAndRemoveSushis(List<Sushi> activeSushis, List<(int typeId, Plate plate)> reserveTypes)
    {
        Vector3 center = collectCenter != null ? collectCenter.position : Vector3.zero;
        int totalCount = activeSushis.Count + reserveTypes.Count;
        int completedCount = 0;

        foreach (var sushi in activeSushis)
        {
            var plate = sushi.CurrentPlate;
            if (plate != null)
            {
                plate.RemoveSpecificSushi(sushi);
            }

            sushi.transform.DOMove(center, 0.5f).SetEase(Ease.InBack);
            sushi.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    SushiLockSystem.Instance?.ClearLockedSushi(sushi);
                    SushiPool.Instance.Return(sushi);

                    completedCount++;
                    if (completedCount >= totalCount)
                    {
                        GameStateChecker.Instance?.CheckWinCondition();
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
                        GameStateChecker.Instance?.CheckWinCondition();
                    }
                });
        }
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