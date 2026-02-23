using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class HintSystem : MonoBehaviour
{
    [SerializeField] private PlateManager plateManager;
    [SerializeField] private float hintDelay = 5f;

    [Header("Hint Animation")]
    [SerializeField] private float riceSqueezeDuration = 0.15f;
    [SerializeField] private float riceSqueezeScale = 0.95f;
    [SerializeField] private float riceStretchScale = 1.05f;
    [SerializeField] private float toppingBounceHeight = 0.3f;
    [SerializeField] private float toppingBounceDuration = 0.4f;
    [SerializeField] private Ease bounceEase = Ease.OutQuad;

    private float idleTimer = 0f;
    private bool isHinting = false;
    private List<Sushi> currentHintSushis = new List<Sushi>();
    private Dictionary<Sushi, Sequence> activeSequences = new Dictionary<Sushi, Sequence>();
    private Dictionary<Sushi, Vector3> originalToppingPositions = new Dictionary<Sushi, Vector3>();

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
        Transform ricePart = sushi.RicePart;
        Transform toppingPart = sushi.ToppingPart;

        if (ricePart == null || toppingPart == null) return;

        Vector3 originalToppingPos = toppingPart.localPosition;
        originalToppingPositions[sushi] = originalToppingPos;

        Sequence hintSequence = DOTween.Sequence();

        hintSequence.Append(
            DOTween.To(() => Vector3.one, x =>
            {
                ricePart.localScale = new Vector3(riceSqueezeScale, riceStretchScale, 1f);
            }, Vector3.one, riceSqueezeDuration)
        );

        hintSequence.Join(
            toppingPart.DOLocalMoveY(originalToppingPos.y + toppingBounceHeight, toppingBounceDuration)
                .SetEase(bounceEase)
        );

        hintSequence.Append(
            DOTween.To(() => Vector3.one, x =>
            {
                ricePart.localScale = Vector3.one;
            }, Vector3.one, riceSqueezeDuration)
        );

        hintSequence.Join(
            toppingPart.DOLocalMoveY(originalToppingPos.y, toppingBounceDuration)
                .SetEase(Ease.OutBounce)
        );

        hintSequence.SetLoops(-1, LoopType.Restart);

        activeSequences[sushi] = hintSequence;
    }

    private void StopHint()
    {
        foreach (var kvp in activeSequences)
        {
            if (kvp.Value != null)
                kvp.Value.Kill();
        }
        activeSequences.Clear();

        if (currentHintSushis.Count > 0)
        {
            foreach (var sushi in currentHintSushis)
            {
                if (sushi != null)
                {
                    if (sushi.RicePart != null)
                    {
                        sushi.RicePart.DOKill();
                        sushi.RicePart.localScale = Vector3.one;
                    }

                    if (sushi.ToppingPart != null)
                    {
                        sushi.ToppingPart.DOKill();
                        sushi.ToppingPart.localPosition = originalToppingPositions.TryGetValue(sushi, out var origPos)
                            ? origPos
                            : sushi.ToppingPart.localPosition;
                    }
                }
            }
            currentHintSushis.Clear();
        }

        originalToppingPositions.Clear();
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