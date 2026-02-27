using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    [SerializeField] private PlateManager plateManager;
    [SerializeField] private Transform collectCenter;
    [SerializeField] private SushiPackagingEffect packagingEffect;
    [SerializeField] private ParticleSystem shuffleVFX;

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
        if (validTypes.Count == 0) return;

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

        bool isValid = false;
        int attempts = 0;

        while (!isValid && attempts < 100)
        {
            attempts++;
            Shuffle(combinedTypes);
            if (ValidateShuffleResult(allActiveSushis.Count, combinedTypes))
                isValid = true;
        }

        if (!isValid)
            ForceFixSameThree(combinedTypes);

        int index = 0;

        foreach (var sushi in allActiveSushis)
        {
            var data = SushiPool.Instance.GetData(combinedTypes[index]);
            if (data != null)
                sushi.Initialize(combinedTypes[index], data.riceSprite, data.toppingSprite, data.sushiType, data.toppingOffsetX, data.toppingOffsetY, data.plateOffsetY);
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
                        layer.SushiTypes[i] = combinedTypes[index++];
                }
            }
        }

        foreach (var plate in plateManager.GetAllPlates())
        {
            if (!plate.gameObject.activeSelf) continue;
            plate.RefreshVisuals();
            plate.UpdateReserveDisplay();
            plate.RecheckMerge();
        }
        shuffleVFX?.Play();

    }

    private bool ValidateShuffleResult(int activeSushiCount, List<int> combinedTypes)
    {
        int index = activeSushiCount;

        var plates = plateManager.GetAllPlates();
        foreach (var plate in plates)
        {
            if (!plate.gameObject.activeSelf) continue;
            if (plate.State == PlateState.LockedAd) continue;

            var layers = plate.GetAllLayers();
            foreach (var layer in layers)
            {
                if (layer.SushiTypes.Count == 3)
                {
                    if (index + 2 >= combinedTypes.Count) return true;

                    if (combinedTypes[index] == combinedTypes[index + 1] &&
                        combinedTypes[index + 1] == combinedTypes[index + 2])
                        return false;
                }
                index += layer.SushiTypes.Count;
            }
        }

        var activeTypes = new List<int>();
        for (int i = 0; i < activeSushiCount && i < combinedTypes.Count; i++)
            activeTypes.Add(combinedTypes[i]);

        foreach (var plate in plates)
        {
            if (!plate.gameObject.activeSelf || plate.IsLocked) continue;

            var plateSushis = GetAllActiveSushis().Where(s => s.CurrentPlate == plate).ToList();
            if (plateSushis.Count == 3)
            {
                for (int i = 0; i <= activeTypes.Count - 3; i++)
                {
                    var checkList = GetAllActiveSushis().Skip(i).Take(3).ToList();
                    if (checkList.Count == 3 && checkList.All(s => s.CurrentPlate == plate))
                    {
                        if (combinedTypes[i] == combinedTypes[i + 1] &&
                            combinedTypes[i + 1] == combinedTypes[i + 2])
                            return false;
                    }
                }
            }
        }

        return true;
    }

    private void ForceFixSameThree(List<int> combinedTypes)
    {
        int activeSushiCount = GetAllActiveSushis().Count;
        int index = activeSushiCount;

        foreach (var plate in plateManager.GetAllPlates())
        {
            if (!plate.gameObject.activeSelf) continue;
            if (plate.State == PlateState.LockedAd) continue;

            var layers = plate.GetAllLayers();
            foreach (var layer in layers)
            {
                if (layer.SushiTypes.Count == 3 && index + 2 < combinedTypes.Count)
                {
                    if (combinedTypes[index] == combinedTypes[index + 1] &&
                        combinedTypes[index + 1] == combinedTypes[index + 2])
                    {
                        for (int swapIdx = 0; swapIdx < combinedTypes.Count; swapIdx++)
                        {
                            if (swapIdx == index + 2) continue;
                            if (combinedTypes[swapIdx] != combinedTypes[index])
                            {
                                (combinedTypes[index + 2], combinedTypes[swapIdx]) = (combinedTypes[swapIdx], combinedTypes[index + 2]);
                                break;
                            }
                        }
                    }
                }
                index += layer.SushiTypes.Count;
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
            onSushiSelected.Invoke(sushi);
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
            reserveRemoved = RemoveTypesFromReserve(targetType, needed);

        foreach (var sushi in sushisToRemove)
        {
            if (sushi.CurrentPlate != null)
                sushi.CurrentPlate.RemoveSpecificSushi(sushi, true, true);
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
                        layer.SlotIndices.RemoveAt(idx);
                    if (lockStages != null && lockStages.Count > idx)
                        lockStages.RemoveAt(idx);
                }

                if (layer.SushiTypes.Count == 0)
                    layersToRemove.Add(layerIdx);
            }

            foreach (var layerIdx in layersToRemove.OrderByDescending(x => x))
                plate.RemoveLayer(layerIdx);

            if (layersToRemove.Count > 0)
                plate.UpdateReserveDisplay();
        }

        return removed;
    }

    private void AnimateAndRemoveSushis(List<Sushi> activeSushis, List<(int typeId, Plate plate)> reserveTypes, HashSet<Plate> platesToCheck)
    {
        Vector3 centerPos = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
        centerPos.z = 0f;

        var tempSushis = new List<Sushi>();
        foreach (var (typeId, plate) in reserveTypes)
        {
            var tempSushi = SushiPool.Instance.Get(typeId);
            tempSushi.transform.position = plate.transform.position + Vector3.down * 0.5f;
            tempSushi.transform.localScale = Vector3.one;
            tempSushis.Add(tempSushi);
        }

        var allSushis = new List<Sushi>(activeSushis);
        allSushis.AddRange(tempSushis);

        if (packagingEffect != null && allSushis.Count == 3)
        {
            packagingEffect.PlayPackagingEffect(centerPos, allSushis, null, () =>
            {
                foreach (var sushi in activeSushis)
                    SushiLockSystem.Instance?.ClearLockedSushi(sushi);
                foreach (var sushi in allSushis)
                    SushiPool.Instance.Return(sushi);
                OnItemAnimationComplete(platesToCheck);
            });
        }
        else
        {
            int totalCount = allSushis.Count;
            int completedCount = 0;

            foreach (var sushi in allSushis)
            {
                sushi.transform.DOMove(centerPos, 0.5f).SetEase(Ease.InBack);
                sushi.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack)
                    .OnComplete(() =>
                    {
                        completedCount++;
                        if (completedCount >= totalCount)
                        {
                            foreach (var s in activeSushis)
                                SushiLockSystem.Instance?.ClearLockedSushi(s);
                            foreach (var s in allSushis)
                                SushiPool.Instance.Return(s);
                            OnItemAnimationComplete(platesToCheck);
                        }
                    });
            }
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
            foreach (var layer in plate.GetAllLayers())
                result.AddRange(layer.SushiTypes);
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