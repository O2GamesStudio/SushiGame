using UnityEngine;
using System.Collections.Generic;

public class SpotlightEffect : MonoBehaviour
{
    [SerializeField] private int sushiOrderBase = 21;

    private List<(SpriteRenderer sr, int originalOrder)> cachedRenderers = new();

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Show(List<Sushi> targetSushis)
    {
        cachedRenderers.Clear();
        gameObject.SetActive(true);

        foreach (var sushi in targetSushis)
        {
            var renderers = sushi.GetComponentsInChildren<SpriteRenderer>();
            int order = sushiOrderBase;
            foreach (var sr in renderers)
            {
                cachedRenderers.Add((sr, sr.sortingOrder));
                sr.sortingOrder = order++;
            }
        }
    }

    public void Hide()
    {
        foreach (var (sr, originalOrder) in cachedRenderers)
        {
            if (sr == null) continue;
            sr.sortingOrder = originalOrder;
        }
        cachedRenderers.Clear();
        gameObject.SetActive(false);
    }
}