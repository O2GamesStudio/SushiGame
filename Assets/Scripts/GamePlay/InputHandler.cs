using UnityEngine;
using DG.Tweening;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlateManager plateManager;
    [SerializeField] private LayerMask sushiLayer;
    [SerializeField] private LayerMask plateLayer;
    [SerializeField] private HintSystem hintSystem;

    private Plate selectedPlate;
    private Sushi draggedSushi;
    private Vector3 originalPosition;
    private RailSlot originRailSlot;
    private Plate _pendingUnlockPlate;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            OnMouseDown();
        else if (Input.GetMouseButton(0) && draggedSushi != null)
            OnMouseDrag();
        else if (Input.GetMouseButtonUp(0) && draggedSushi != null)
            OnMouseUp();
    }

    private void OnMouseDown()
    {
        InGameTutorialController.Instance?.OnScreenTouched();
        if (GameManager.Instance != null && !GameManager.Instance.IsTimerStarted)
            GameManager.Instance.StartTimer();

        hintSystem?.ResetTimer();

        var sushi = GetSushiAtMousePosition();
        if (sushi != null)
        {
            if (ItemManager.Instance != null && ItemManager.Instance.IsWaitingForTarget)
            {
                ItemManager.Instance.OnSushiClicked(sushi);
                return;
            }

            if (sushi.IsLocked) return;

            var rail = plateManager.Rail;
            if (rail != null)
            {
                var railSlot = rail.GetSlotContainingSushi(sushi);
                if (railSlot != null)
                {
                    originRailSlot = railSlot;
                    railSlot.ClearSushi();
                    draggedSushi = sushi;
                    originalPosition = sushi.transform.position;
                    sushi.transform.SetParent(null);
                    draggedSushi.SetDragState(true);
                    return;
                }
            }

            selectedPlate = GetPlateContainingSushi(sushi);
            if (selectedPlate != null && !selectedPlate.IsLocked)
            {
                draggedSushi = sushi;
                originalPosition = draggedSushi.transform.position;
                draggedSushi.SetDragState(true);
            }
            return;
        }

        var clickedPlate = GetPlateAtMousePosition();
        if (clickedPlate != null && clickedPlate.IsLocked)
        {
            if (clickedPlate.State == PlateState.LockedAd)
            {
                if (UnityAdsManager.Instance == null) return;

                UnityAdsManager.Instance.OnRewardEarned += OnAdPlateUnlockRewardEarned;
                UnityAdsManager.Instance.OnAdFailedToShow += OnAdPlateUnlockFailed;

                _pendingUnlockPlate = clickedPlate;
                UnityAdsManager.Instance.ShowRewardedAd();
            }
        }
    }
    private void OnAdPlateUnlockRewardEarned()
    {
        UnityAdsManager.Instance.OnRewardEarned -= OnAdPlateUnlockRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow -= OnAdPlateUnlockFailed;

        if (_pendingUnlockPlate != null)
        {
            PlateUnlockSystem.Instance?.TryUnlockAdPlate(_pendingUnlockPlate);
            _pendingUnlockPlate = null;
        }
    }

    private void OnAdPlateUnlockFailed()
    {
        UnityAdsManager.Instance.OnRewardEarned -= OnAdPlateUnlockRewardEarned;
        UnityAdsManager.Instance.OnAdFailedToShow -= OnAdPlateUnlockFailed;
        _pendingUnlockPlate = null;
    }

    private void OnMouseDrag()
    {
        var worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = -1;
        draggedSushi.transform.position = worldPos;
    }

    private void OnMouseUp()
    {
        var dropPosition = draggedSushi.transform.position;
        var sushi = draggedSushi;
        var fromPlate = selectedPlate;
        var returnPos = originalPosition;
        var fromRailSlot = originRailSlot;

        draggedSushi = null;
        selectedPlate = null;
        originRailSlot = null;

        if (fromRailSlot != null)
        {
            var targetPlateForRail = GetPlateOverlappingWithSushi(sushi);
            HandleRailDrop(sushi, targetPlateForRail, dropPosition, fromRailSlot, returnPos);
            return;
        }

        var targetPlate = GetPlateOverlappingWithSushi(sushi);

        if (targetPlate != null)
        {
            if (targetPlate == fromPlate)
            {
                int closestSlot = targetPlate.GetClosestSlotIncludingCurrent(dropPosition, sushi);
                if (closestSlot >= 0)
                {
                    sushi.SetDragState(false);
                    if (!fromPlate.MoveSushiWithinPlate(sushi, closestSlot))
                        ReturnToPosition(sushi, returnPos);
                }
                else
                {
                    ReturnToPosition(sushi, returnPos);
                }
            }
            else
            {
                if (plateManager.CanMoveSushi(fromPlate, targetPlate))
                {
                    sushi.SetDragState(false);
                    plateManager.MoveSushi(fromPlate, targetPlate, sushi, dropPosition);
                }
                else
                {
                    ReturnToPosition(sushi, returnPos);
                }
            }
        }
        else
        {
            ReturnToPosition(sushi, returnPos);
        }
    }
    private Plate GetPlateOverlappingWithSushi(Sushi sushi)
    {
        var collider = sushi.GetComponent<CircleCollider2D>();
        if (collider == null) return null;

        var results = new Collider2D[5];
        var filter = new ContactFilter2D { layerMask = plateLayer, useLayerMask = true };
        int count = Physics2D.OverlapCollider(collider, filter, results);

        Plate closestPlate = null;
        float minDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            if (results[i] == null) continue;
            var plate = results[i].GetComponent<Plate>();
            if (plate == null) continue;
            float dist = Vector2.Distance(sushi.transform.position, plate.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestPlate = plate;
            }
        }
        return closestPlate;
    }

    private void HandleRailDrop(Sushi sushi, Plate targetPlate, Vector3 dropPosition, RailSlot fromSlot, Vector3 returnPos)
    {
        if (targetPlate != null && !targetPlate.IsLocked && !targetPlate.IsFull)
        {
            sushi.SetDragState(false);
            int preferredSlot = targetPlate.GetClosestEmptySlot(dropPosition);
            targetPlate.AddSushi(sushi, preferredSlot);
        }
        else
        {
            plateManager.Rail?.ReturnSushiToSlot(sushi, fromSlot);
        }
    }

    private void ReturnToPosition(Sushi sushi, Vector3 returnPos)
    {
        sushi.transform.DOMove(returnPos, 0.2f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => sushi.SetDragState(false));
    }

    private Sushi GetSushiAtMousePosition()
    {
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        var hit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity, sushiLayer);
        return hit.collider != null ? hit.collider.GetComponent<Sushi>() : null;
    }

    private Plate GetPlateAtMousePosition()
    {
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        var hit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity, plateLayer);
        return hit.collider != null ? hit.collider.GetComponent<Plate>() : null;
    }

    private Plate GetPlateContainingSushi(Sushi sushi)
    {
        var plates = plateManager.GetAllPlates();
        foreach (var plate in plates)
        {
            if (plate.gameObject.activeSelf && plate.ContainsSushi(sushi))
                return plate;
        }
        return null;
    }
}