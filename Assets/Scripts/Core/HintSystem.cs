using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class HintSystem : MonoBehaviour
{
    [SerializeField] private PlateManager plateManager;
    [SerializeField] private float hintDelay = 5f;

    [Header("Hint Animation")]
    [SerializeField] private float stretchDuration = 0.3f;
    [SerializeField] private float stretchScale = 1.15f;
    [SerializeField] private Ease stretchEase = Ease.InOutSine;

    private float idleTimer = 0f;
    private bool isHinting = false;
    private List<Sushi> currentHintSushis = new List<Sushi>();
    private Dictionary<Sushi, Sequence> activeSequences = new Dictionary<Sushi, Sequence>();

    private void Update()
    {
        if (!isHinting)
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
            isHinting = true;
            currentHintSushis = mergeableSet;

            foreach (var sushi in currentHintSushis)
            {
                if (sushi != null && sushi.gameObject.activeSelf)
                {
                    PlayStretchAnimation(sushi);
                }
            }
        }
        else
        {
            idleTimer = 0f;
        }
    }

    private void PlayStretchAnimation(Sushi sushi)
    {
        float squashScale = 1f / stretchScale;

        Sequence stretchSequence = DOTween.Sequence();

        stretchSequence.Append(sushi.transform.DOScale(new Vector3(stretchScale, squashScale, 1f), stretchDuration).SetEase(stretchEase));
        stretchSequence.Append(sushi.transform.DOScale(Vector3.one, stretchDuration).SetEase(stretchEase));
        stretchSequence.Append(sushi.transform.DOScale(new Vector3(squashScale, stretchScale, 1f), stretchDuration).SetEase(stretchEase));
        stretchSequence.Append(sushi.transform.DOScale(Vector3.one, stretchDuration).SetEase(stretchEase));

        stretchSequence.SetLoops(-1, LoopType.Restart);

        activeSequences[sushi] = stretchSequence;
    }

    private void StopHint()
    {
        foreach (var kvp in activeSequences)
        {
            if (kvp.Value != null)
            {
                kvp.Value.Kill();
            }
        }
        activeSequences.Clear();

        if (currentHintSushis.Count > 0)
        {
            foreach (var sushi in currentHintSushis)
            {
                if (sushi != null && sushi.transform != null)
                {
                    sushi.transform.DOKill();
                    sushi.transform.localScale = Vector3.one;
                }
            }
            currentHintSushis.Clear();
        }

        isHinting = false;
    }

    private List<Sushi> FindMergeableSet()
    {
        if (plateManager == null) return null;

        var plates = plateManager.GetAllPlates();
        if (plates == null) return null;

        var sushisByType = new Dictionary<int, List<Sushi>>();

        foreach (var plate in plates)
        {
            if (plate == null || !plate.gameObject.activeSelf || plate.IsLocked) continue;

            var activeSushis = plate.GetActiveSushis();
            if (activeSushis == null) continue;

            foreach (var sushi in activeSushis)
            {
                if (sushi == null || !sushi.gameObject.activeSelf || sushi.IsLocked) continue;

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