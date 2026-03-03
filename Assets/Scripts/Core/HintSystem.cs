using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class HintSystem : MonoBehaviour
{
    [SerializeField] private PlateManager plateManager;
    [SerializeField] private float hintDelay = 5f;

    [Header("Hint Animation")]
    [SerializeField] private float toppingBounceHeight = 0.3f;
    [SerializeField] private float toppingBounceDuration = 0.4f;

    [Header("Integrated  Hint Animation")]
    [SerializeField] private float unitScalePunch = 0.15f;
    [SerializeField] private float unitScaleDuration = 0.4f;
    [SerializeField] private float unitShakeStrength = 0.08f;

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
                ShowHint();
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
                    PlayHintAnimation(sushi);
            }
        }
        else
        {
            idleTimer = 0f;
        }
    }

    private void PlayHintAnimation(Sushi sushi)
    {
        if (sushi.SushiType == SushiType.Integrated)
            PlayUnitAnimation(sushi);
        else
            PlayToppingAnimation(sushi);
    }

    private void PlayToppingAnimation(Sushi sushi)
    {
        Transform toppingPart = sushi.ToppingPart;
        if (toppingPart == null) return;

        Vector3 originalToppingPos = toppingPart.localPosition;
        originalToppingPositions[sushi] = originalToppingPos;

        var toppingRenderer = toppingPart.GetComponent<SpriteRenderer>();
        int originalOrder = toppingRenderer != null ? toppingRenderer.sortingOrder : 0;

        if (toppingRenderer != null)
            toppingRenderer.sortingOrder = originalOrder + 1;

        Sequence seq = DOTween.Sequence();
        seq.Append(toppingPart.DOLocalMoveY(originalToppingPos.y + toppingBounceHeight, 0.2f).SetEase(Ease.OutQuad));
        seq.Append(toppingPart.DOShakePosition(0.3f, new Vector3(0.05f, 0.02f, 0f), 15, 0f, false, false));
        seq.Append(toppingPart.DOLocalMove(originalToppingPos, toppingBounceDuration).SetEase(Ease.OutBounce));
        seq.AppendInterval(0.3f);
        seq.SetLoops(-1, LoopType.Restart);
        seq.OnKill(() =>
        {
            if (toppingRenderer != null)
                toppingRenderer.sortingOrder = originalOrder;
        });

        activeSequences[sushi] = seq;
    }

    private void PlayUnitAnimation(Sushi sushi)
    {
        Transform ricePart = sushi.RicePart;
        if (ricePart == null) return;

        Vector3 originalPos = ricePart.localPosition;
        originalToppingPositions[sushi] = originalPos;

        var riceRenderer = ricePart.GetComponent<SpriteRenderer>();
        int originalOrder = riceRenderer != null ? riceRenderer.sortingOrder : 0;

        if (riceRenderer != null)
            riceRenderer.sortingOrder = originalOrder + 1;

        Sequence seq = DOTween.Sequence();
        seq.Append(ricePart.DOLocalMoveY(originalPos.y + toppingBounceHeight, 0.2f).SetEase(Ease.OutQuad));
        seq.Append(ricePart.DOShakePosition(0.3f, new Vector3(0.05f, 0.02f, 0f), 15, 0f, false, false));
        seq.Append(ricePart.DOLocalMove(originalPos, toppingBounceDuration).SetEase(Ease.OutBounce));
        seq.AppendInterval(0.3f);
        seq.SetLoops(-1, LoopType.Restart);
        seq.OnKill(() =>
        {
            if (riceRenderer != null)
                riceRenderer.sortingOrder = originalOrder;
        });

        activeSequences[sushi] = seq;
    }

    private void StopHint()
    {
        foreach (var kvp in activeSequences)
            kvp.Value?.Kill();
        activeSequences.Clear();

        foreach (var sushi in currentHintSushis)
        {
            if (sushi == null) continue;

            if (sushi.SushiType == SushiType.Integrated)
            {
                if (sushi.RicePart != null)
                {
                    sushi.RicePart.DOKill();
                    sushi.RicePart.localPosition = originalToppingPositions.TryGetValue(sushi, out var origPos)
                        ? origPos
                        : sushi.RicePart.localPosition;
                }
            }
            else
            {
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
                    sushisByType[sushi.TypeId] = new List<Sushi>();

                sushisByType[sushi.TypeId].Add(sushi);
            }
        }

        foreach (var kvp in sushisByType)
        {
            if (kvp.Value.Count >= 3)
                return kvp.Value.OrderBy(x => Random.value).Take(3).ToList();
        }

        return null;
    }
}