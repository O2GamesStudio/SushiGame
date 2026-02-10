using UnityEngine;
using System.Collections.Generic;

public class PlateUnlockSystem : MonoBehaviour
{
    public static PlateUnlockSystem Instance { get; private set; }

    [SerializeField] private PlateManager plateManager;

    private Dictionary<Plate, int> lockedSushiPlates = new Dictionary<Plate, int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterLockedPlate(Plate plate, PlateState lockType, int requiredSushiTypeId = -1)
    {
        plate.SetState(lockType, requiredSushiTypeId);

        if (lockType == PlateState.LockedSushi && requiredSushiTypeId >= 0)
        {
            lockedSushiPlates[plate] = requiredSushiTypeId;
        }
    }

    public void UnlockAdPlate(Plate plate)
    {
        if (plate.State == PlateState.LockedAd)
        {
            Debug.Log("[PlateUnlockSystem] 광고 시청 완료 - Plate 해제");
            plate.Unlock();
        }
    }

    public void OnSushiMerged(int mergedTypeId)
    {
        var platesToUnlock = new List<Plate>();

        foreach (var kvp in lockedSushiPlates)
        {
            if (kvp.Value == mergedTypeId)
            {
                platesToUnlock.Add(kvp.Key);
            }
        }

        foreach (var plate in platesToUnlock)
        {
            plate.Unlock();
            lockedSushiPlates.Remove(plate);
        }
    }

    public void TryUnlockAdPlate(Plate plate)
    {
        if (plate.State == PlateState.LockedAd)
        {
            UnlockAdPlate(plate);
        }
    }
}