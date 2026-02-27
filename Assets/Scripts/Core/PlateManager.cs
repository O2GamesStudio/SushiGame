using System.Collections.Generic;
using UnityEngine;

public class PlateManager : MonoBehaviour
{
    [SerializeField] private List<Plate> plates;

    public void Initialize(List<PlateData> plateDataList, bool sequentialActivation = false)
    {
        for (int i = 0; i < plates.Count; i++)
            plates[i].gameObject.SetActive(false);

        List<int> plateIndices = new List<int>();

        if (sequentialActivation)
        {
            for (int i = 0; i < plateDataList.Count; i++)
                plateIndices.Add(i);
        }
        else
        {
            for (int i = 0; i < plates.Count; i++)
                plateIndices.Add(i);

            for (int i = plateIndices.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (plateIndices[i], plateIndices[j]) = (plateIndices[j], plateIndices[i]);
            }
        }

        for (int i = 0; i < plateDataList.Count; i++)
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
        return true;
    }

    public List<Plate> GetAllPlates() => plates;
}