using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using System;

public class SushiPackagingEffect : MonoBehaviour
{
    [Header("Packaging Sprites")]
    [SerializeField] private Sprite containerSprite;
    [SerializeField] private Sprite maskSprite;
    [SerializeField] private Sprite lidSprite;

    [Header("Position Settings")]
    [SerializeField] private Vector3 containerOffset = Vector3.zero;
    [SerializeField] private Vector3 maskOffset = Vector3.zero;
    [SerializeField] private Vector3 lidOffset = Vector3.zero;
    [SerializeField] private float sushiSpacing = 0.6f;

    [Header("Animation Settings")]
    [SerializeField] private float sushiContainerScale = 0.7f;
    [SerializeField] private float sushiMoveToContainerDuration = 0.3f;
    [SerializeField] private float lidDropDuration = 0.3f;
    [SerializeField] private float lidDropDistance = 1.5f;
    [SerializeField] private float packageDisappearDelay = 0.2f;
    [SerializeField] private float packageDisappearDuration = 0.3f;

    [Header("Sorting Order")]
    [SerializeField] private int containerSortingOrder = 50;
    [SerializeField] private int maskSortingOrder = 51;
    [SerializeField] private int sushiInContainerSortingOrder = 52;
    [SerializeField] private int lidSortingOrder = 54;

    public void PlayPackagingEffect(Vector3 platePosition, List<Sushi> sushis, Action<Vector3> onSushisArrived, Action onComplete)
    {
        if (containerSprite == null || lidSprite == null || sushis.Count != 3)
        {
            onSushisArrived?.Invoke(platePosition);
            onComplete?.Invoke();
            return;
        }

        Vector3 containerPosition = platePosition + containerOffset;
        GameObject container = CreateContainer(containerPosition);
        GameObject mask = CreateMask(containerPosition);

        MoveSushisToContainer(sushis, containerPosition, () =>
        {
            onSushisArrived?.Invoke(containerPosition);
            DropLidAndComplete(container, mask, containerPosition, sushis, onComplete);
        });
    }

    private void DropLidAndComplete(GameObject container, GameObject mask, Vector3 position, List<Sushi> sushis, Action onComplete)
    {
        foreach (var sushi in sushis)
            SetSushiSortingOrder(sushi, sushiInContainerSortingOrder);

        GameObject lid = CreateLid(position);

        lid.transform.DOMove(position + lidOffset, lidDropDuration)
            .SetEase(Ease.OutBounce)
            .OnComplete(() =>
            {
                DOVirtual.DelayedCall(packageDisappearDelay, () =>
                {
                    DisappearPackage(container, mask, lid, sushis, onComplete);
                });
            });
    }

    private GameObject CreateContainer(Vector3 position)
    {
        var containerObj = new GameObject("SushiContainer");
        containerObj.transform.position = position;

        var renderer = containerObj.AddComponent<SpriteRenderer>();
        renderer.sprite = containerSprite;
        renderer.sortingLayerName = "Sushi";
        renderer.sortingOrder = containerSortingOrder;

        return containerObj;
    }

    private GameObject CreateMask(Vector3 position)
    {
        var maskObj = new GameObject("SushiContainerMask");
        maskObj.transform.position = position + maskOffset;

        var renderer = maskObj.AddComponent<SpriteRenderer>();
        renderer.sprite = maskSprite;
        renderer.sortingLayerName = "Sushi";
        renderer.sortingOrder = maskSortingOrder;

        return maskObj;
    }

    private void MoveSushisToContainer(List<Sushi> sushis, Vector3 containerPosition, Action onComplete)
    {
        int completedCount = 0;

        for (int i = 0; i < sushis.Count; i++)
        {
            var sushi = sushis[i];
            float xOffset = (i - 1) * sushiSpacing;
            Vector3 targetPosition = containerPosition + new Vector3(xOffset, sushi.PlateOffsetY, 0f);

            sushi.transform.SetParent(null);
            SetSushiSortingOrder(sushi, sushiInContainerSortingOrder);

            targetPosition.z = 0f;

            sushi.transform.DOMove(targetPosition, sushiMoveToContainerDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    completedCount++;
                    if (completedCount >= sushis.Count)
                        onComplete?.Invoke();
                });

            sushi.transform.DOScale(Vector3.one * sushiContainerScale, sushiMoveToContainerDuration)
                .SetEase(Ease.InQuad);
        }
    }

    private void SetSushiSortingOrder(Sushi sushi, int baseOrder)
    {
        sushi.SetBaseSortingOrder("Sushi", baseOrder);
    }

    private GameObject CreateLid(Vector3 position)
    {
        var lidObj = new GameObject("SushiLid");
        lidObj.transform.position = position + lidOffset + Vector3.up * lidDropDistance;

        var renderer = lidObj.AddComponent<SpriteRenderer>();
        renderer.sprite = lidSprite;
        renderer.sortingLayerName = "Sushi";
        renderer.sortingOrder = lidSortingOrder;

        return lidObj;
    }

    private void DisappearPackage(GameObject container, GameObject mask, GameObject lid, List<Sushi> sushis, Action onComplete)
    {
        container.transform.DOScale(Vector3.zero, packageDisappearDuration).SetEase(Ease.InBack);
        mask.transform.DOScale(Vector3.zero, packageDisappearDuration).SetEase(Ease.InBack);
        lid.transform.DOScale(Vector3.zero, packageDisappearDuration).SetEase(Ease.InBack);

        int completedCount = 0;
        int totalCount = sushis.Count + 3;

        foreach (var sushi in sushis)
        {
            sushi.transform.DOScale(Vector3.zero, packageDisappearDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    completedCount++;
                    if (completedCount >= totalCount)
                    {
                        Destroy(container);
                        Destroy(mask);
                        Destroy(lid);
                        onComplete?.Invoke();
                    }
                });
        }

        DOVirtual.DelayedCall(packageDisappearDuration, () =>
        {
            completedCount += 3;
            if (completedCount >= totalCount)
            {
                Destroy(container);
                Destroy(mask);
                Destroy(lid);
                onComplete?.Invoke();
            }
        });
    }
}