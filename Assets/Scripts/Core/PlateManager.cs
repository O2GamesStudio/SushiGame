using System.Collections.Generic;
using UnityEngine;

public class PlateManager : MonoBehaviour
{
    [SerializeField] private List<Plate> plates;
    [SerializeField] private GameObject railPrefab;
    [SerializeField] private GameObject railSlotPrefab;
    private int savedRailRowIndex = 0;

    private RailCtrl railCtrl;
    private List<int> plateIndexMapping = new List<int>();

    public RailCtrl Rail => railCtrl;

    public void Initialize(List<PlateData> plateDataList, bool sequentialActivation = false, RailData railData = null)
    {
        plateIndexMapping.Clear();

        for (int i = 0; i < plates.Count; i++)
            plates[i].gameObject.SetActive(false);

        var railPlateIndices = new HashSet<int>();
        if (railData != null)
        {
            int rowStart = railData.RowIndex * 3;
            railPlateIndices.Add(rowStart);
            railPlateIndices.Add(rowStart + 1);
            railPlateIndices.Add(rowStart + 2);
            SpawnRail(railData, rowStart);
        }

        var plateIndices = new List<int>();

        if (sequentialActivation)
        {
            for (int i = 0; i < plates.Count; i++)
                if (!railPlateIndices.Contains(i))
                    plateIndices.Add(i);
        }
        else
        {
            var available = new List<int>();
            for (int i = 0; i < plates.Count; i++)
                if (!railPlateIndices.Contains(i))
                    available.Add(i);

            for (int i = available.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (available[i], available[j]) = (available[j], available[i]);
            }

            plateIndices = available;
        }

        for (int i = 0; i < plateDataList.Count && i < plateIndices.Count; i++)
        {
            int plateIndex = plateIndices[i];
            var data = plateDataList[i];

            plates[plateIndex].gameObject.SetActive(true);
            plates[plateIndex].Initialize(
                data.ActiveTypes,
                data.Layers,
                data.ActiveLockStages,
                data.SlotCount
            );

            plateIndexMapping.Add(plateIndex);

            if (data.State == PlateState.CantMerge)
            {
                plates[plateIndex].SetState(PlateState.CantMerge);
            }
            else if (data.State != PlateState.Normal)
            {
                PlateUnlockSystem.Instance?.RegisterLockedPlate(
                    plates[plateIndex],
                    data.State,
                    data.RequiredSushiTypeId
                );
            }
        }
    }
    public void RestoreFromSaveData(GameSaveData saveData, LevelData levelData)
    {
        plateIndexMapping.Clear();

        for (int i = 0; i < plates.Count; i++)
            plates[i].gameObject.SetActive(false);

        foreach (var ps in saveData.plateStates)
        {
            if (ps.plateIndex < 0 || ps.plateIndex >= plates.Count) continue;

            var layers = new List<Layer>();
            foreach (var ls in ps.layers)
            {
                var layer = new Layer(ls.sushiTypes);
                for (int i = 0; i < ls.lockStages.Count; i++)
                    layer.SetLockStage(i, ls.lockStages[i]);
                for (int i = 0; i < ls.hiddenStates.Count; i++)
                    layer.SetHiddenState(i, ls.hiddenStates[i]);
                layers.Add(layer);
            }

            var validActiveTypes = ps.activeTypes.FindAll(t => t != -1);

            plates[ps.plateIndex].gameObject.SetActive(true);
            plates[ps.plateIndex].Initialize(
                validActiveTypes,
                layers,
                ps.activeLockStages,
                ps.slotCount
            );

            plateIndexMapping.Add(ps.plateIndex);

            var state = (PlateState)ps.state;
            if (state == PlateState.CantMerge)
            {
                plates[ps.plateIndex].SetState(PlateState.CantMerge);
            }
            else if (state != PlateState.Normal)
            {
                PlateUnlockSystem.Instance?.RegisterLockedPlate(
                    plates[ps.plateIndex],
                    state,
                    ps.requiredSushiTypeId
                );
            }
        }

        if (saveData.railState != null && levelData.isRail)
        {
            int rowIndex = saveData.railState.rowIndex;
            int rowStart = rowIndex * 3;
            var railData = new RailData
            {
                RowIndex = rowIndex,
                SushiTypeIds = saveData.railState.sushiTypeIds,
                RailPlateSprite = levelData.railPlateSprite
            };
            SpawnRail(railData, rowStart);
        }
    }

    public GameSaveData GetSaveData(int stageIndex, float timeRemaining, int mergedSetsCount)
    {
        var saveData = new GameSaveData
        {
            stageIndex = stageIndex,
            timeRemaining = timeRemaining,
            mergedSetsCount = mergedSetsCount
        };

        for (int i = 0; i < plateIndexMapping.Count; i++)
        {
            int plateIndex = plateIndexMapping[i];
            var plate = plates[plateIndex];
            if (!plate.gameObject.activeSelf) continue;

            var ps = new PlateSaveData
            {
                plateIndex = plateIndex,
                activeTypes = plate.GetActiveTypes(),
                activeLockStages = plate.GetActiveLockStages(),
                slotCount = plate.SlotCount,
                state = (int)plate.CurrentState,
                requiredSushiTypeId = plate.RequiredSushiTypeId
            };

            foreach (var layer in plate.GetLayers())
            {
                ps.layers.Add(new LayerSaveData
                {
                    sushiTypes = layer.GetAllTypes(),
                    lockStages = layer.GetLockStages(),
                    hiddenStates = layer.GetHiddenStates()
                });
            }

            saveData.plateStates.Add(ps);
        }

        if (railCtrl != null)
        {
            var railSave = new RailSaveData();
            railSave.rowIndex = savedRailRowIndex;
            foreach (var slot in railCtrl.Slots)
            {
                if (slot.OccupiedSushi != null)
                    railSave.sushiTypeIds.Add(slot.OccupiedSushi.TypeId);
            }
            saveData.railState = railSave;
        }

        return saveData;
    }

    private void SpawnRail(RailData railData, int rowStart)
    {
        if (railPrefab == null || railSlotPrefab == null) return;

        savedRailRowIndex = railData.RowIndex;
        Vector3 centerPos = plates[rowStart + 1].transform.position;
        var railObj = Instantiate(railPrefab, centerPos, Quaternion.identity);
        railCtrl = railObj.GetComponent<RailCtrl>();
        railCtrl?.Initialize(railData.SushiTypeIds, railData.RailPlateSprite, railSlotPrefab);
    }

    public bool CanMoveSushi(Plate from, Plate to)
    {
        return from.ActiveCount > 0 && !to.IsFull && !from.IsLocked && !to.IsLocked;
    }

    public void MoveSushi(Plate from, Plate to, Sushi sushi, Vector3 dropPosition)
    {
        if (!CanMoveSushi(from, to)) return;

        if (from.RemoveSpecificSushi(sushi))
        {
            int preferredSlot = to.GetClosestEmptySlot(dropPosition);
            to.AddSushi(sushi, preferredSlot);
        }
    }

    public bool AreAllPlatesEmpty()
    {
        foreach (var plate in plates)
        {
            if (plate.gameObject.activeSelf && !plate.IsEmpty) return false;
        }

        if (railCtrl != null)
        {
            foreach (var slot in railCtrl.Slots)
            {
                if (!slot.IsEmpty) return false;
            }
        }

        return true;
    }

    public List<Plate> GetAllPlates() => plates;
}