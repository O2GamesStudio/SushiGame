using UnityEngine;
using System.Collections.Generic;

public class SushiLockSystem : MonoBehaviour
{
    public static SushiLockSystem Instance { get; private set; }

    private int totalMergeCount = 0;
    private HashSet<Sushi> lockedSushis = new HashSet<Sushi>();

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

    public void RegisterLockedSushi(Sushi sushi, int initialStage = 3)
    {
        sushi.SetLockStage(initialStage);
        lockedSushis.Add(sushi);
    }

    public void OnMergeCompleted()
    {
        totalMergeCount++;

        var sushisToUpdate = new List<Sushi>(lockedSushis);
        foreach (var sushi in sushisToUpdate)
        {
            if (sushi != null && sushi.IsLocked)
            {
                sushi.DecreaseLockStage();

                if (!sushi.IsLocked)
                {
                    lockedSushis.Remove(sushi);
                }
            }
        }
    }

    public void ClearLockedSushi(Sushi sushi)
    {
        lockedSushis.Remove(sushi);
    }

    public void Reset()
    {
        totalMergeCount = 0;
        lockedSushis.Clear();
    }
}