using System.Collections.Generic;
using UnityEngine;

public class PlateManager : MonoBehaviour
{
    [SerializeField] private List<Plate> plates;
    [SerializeField] private GameObject railPrefab;
    [SerializeField] private GameObject railSlotPrefab;

    private RailCtrl railCtrl;

    public RailCtrl Rail => railCtrl;

    public void Initialize(List<PlateData> plateDataList, bool sequentialActivation = false, RailData railData = null)
    {
        for (int i = 0; i < plates.Count; i++)
            plates[i].gameObject.SetActive(false);

        HashSet<int> railPlateIndices = new HashSet<int>();
        if (railData != null)
        {
            int rowStart = railData.RowIndex * 3;
            railPlateIndices.Add(rowStart);
            railPlateIndices.Add(rowStart + 1);
            railPlateIndices.Add(rowStart + 2);

            SpawnRail(railData, rowStart);
        }

        List<int> plateIndices = new List<int>();

        if (sequentialActivation)
        {
            for (int i = 0; i < plates.Count; i++)
            {
                if (!railPlateIndices.Contains(i))
                    plateIndices.Add(i);
            }
        }
        else
        {
            List<int> available = new List<int>();
            for (int i = 0; i < plates.Count; i++)
            {
                if (!railPlateIndices.Contains(i))
                    available.Add(i);
            }

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

            if (data.State != PlateState.Normal)
            {
                PlateUnlockSystem.Instance?.RegisterLockedPlate(
                    plates[plateIndex],
                    data.State,
                    data.RequiredSushiTypeId
                );
            }
        }
    }

    private void SpawnRail(RailData railData, int rowStart)
    {
        if (railPrefab == null || railSlotPrefab == null) return;

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