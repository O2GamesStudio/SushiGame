using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class HintSystem : MonoBehaviour
{
    [SerializeField] private PlateManager plateManager;
    [SerializeField] private float hintDelay = 5f;
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeStrength = 0.05f;
    [SerializeField] private int shakeVibrato = 10;

    private float idleTimer = 0f;
    private bool isShaking = false;
    private List<Sushi> currentHintSushis = new List<Sushi>();

    private void Update()
    {
        if (!isShaking)
        {
            idleTimer += Time.deltaTime;

            if (idleTimer >= hintDelay)
            {
                ShowHint();
            }
        }
    }

    public void ResetTimer()
    {
        idleTimer = 0f;
        StopHint();
    }

    private void ShowHint()
    {
        var mergeableSet = FindMergeableSet();

        if (mergeableSet != null && mergeableSet.Count == 3)
        {
            isShaking = true;
            currentHintSushis = mergeableSet;

            foreach (var sushi in currentHintSushis)
            {
                sushi.transform.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato)
                    .SetLoops(-1, LoopType.Restart);
            }
        }
        else
        {
            idleTimer = 0f;
        }
    }

    private void StopHint()
    {
        if (currentHintSushis.Count > 0)
        {
            foreach (var sushi in currentHintSushis)
            {
                if (sushi != null)
                {
                    sushi.transform.DOKill();
                }
            }
            currentHintSushis.Clear();
        }
        isShaking = false;
    }

    private List<Sushi> FindMergeableSet()
    {
        var plates = plateManager.GetAllPlates();
        var sushisByType = new Dictionary<int, List<Sushi>>();

        foreach (var plate in plates)
        {
            if (!plate.gameObject.activeSelf) continue;

            var activeSushis = plate.GetActiveSushis();

            foreach (var sushi in activeSushis)
            {
                if (!sushisByType.ContainsKey(sushi.TypeId))
                {
                    sushisByType[sushi.TypeId] = new List<Sushi>();
                }
                sushisByType[sushi.TypeId].Add(sushi);
            }
        }

        foreach (var kvp in sushisByType)
        {
            if (kvp.Value.Count >= 3)
            {
                var shuffled = kvp.Value.OrderBy(x => Random.value).Take(3).ToList();
                return shuffled;
            }
        }

        return null;
    }
}