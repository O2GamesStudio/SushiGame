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
    private SpriteRenderer draggedSushiRenderer;
    private int originalSortingOrder;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            OnMouseDown();
        }
        else if (Input.GetMouseButton(0) && draggedSushi != null)
        {
            OnMouseDrag();
        }
        else if (Input.GetMouseButtonUp(0) && draggedSushi != null)
        {
            OnMouseUp();
        }
    }
    private void OnMouseDown()
    {
        hintSystem?.ResetTimer();

        var sushi = GetSushiAtMousePosition();
        if (sushi != null)
        {
            selectedPlate = GetPlateContainingSushi(sushi);
            if (selectedPlate != null)
            {
                draggedSushi = sushi;
                originalPosition = draggedSushi.transform.position;

                draggedSushiRenderer = draggedSushi.GetComponent<SpriteRenderer>();
                if (draggedSushiRenderer != null)
                {
                    originalSortingOrder = draggedSushiRenderer.sortingOrder;
                    draggedSushiRenderer.sortingOrder = 100;
                }

                draggedSushi.SetDragState(true);
            }
        }
    }

    private void OnMouseDrag()
    {
        var worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = -1;
        draggedSushi.transform.position = worldPos;
    }
    private void OnMouseUp()
    {
        var targetPlate = GetPlateAtMousePosition();
        var dropPosition = draggedSushi.transform.position;

        if (targetPlate != null)
        {
            if (targetPlate == selectedPlate)
            {
                int closestSlot = targetPlate.GetClosestEmptySlot(dropPosition);
                if (closestSlot >= 0)
                {
                    selectedPlate.RemoveSpecificSushi(draggedSushi);
                    targetPlate.AddSushi(draggedSushi, closestSlot);
                    RestoreSortingOrder();
                    draggedSushi.SetDragState(false);
                }
                else
                {
                    ReturnToOriginalPosition();
                }
            }
            else
            {
                if (plateManager.CanMoveSushi(selectedPlate, targetPlate))
                {
                    plateManager.MoveSushi(selectedPlate, targetPlate, draggedSushi, dropPosition);
                    RestoreSortingOrder();
                    draggedSushi.SetDragState(false);
                }
                else
                {
                    ReturnToOriginalPosition();
                }
            }
        }
        else
        {
            ReturnToOriginalPosition();
        }

        draggedSushi = null;
        selectedPlate = null;
        draggedSushiRenderer = null;
    }

    private void ReturnToOriginalPosition()
    {
        draggedSushi.transform.DOMove(originalPosition, 0.2f).SetEase(Ease.OutQuad);
        draggedSushi.SetDragState(false);
        RestoreSortingOrder();
    }

    private void RestoreSortingOrder()
    {
        if (draggedSushiRenderer != null)
        {
            draggedSushiRenderer.sortingOrder = originalSortingOrder;
        }
    }

    private Sushi GetSushiAtMousePosition()
    {
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        var hit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity, sushiLayer);

        if (hit.collider != null)
        {
            return hit.collider.GetComponent<Sushi>();
        }

        return null;
    }

    private Plate GetPlateAtMousePosition()
    {
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        var hit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity, plateLayer);

        if (hit.collider != null)
        {
            return hit.collider.GetComponent<Plate>();
        }

        return null;
    }

    private Plate GetPlateContainingSushi(Sushi sushi)
    {
        var plates = plateManager.GetAllPlates();
        foreach (var plate in plates)
        {
            if (plate.gameObject.activeSelf && plate.ContainsSushi(sushi))
            {
                return plate;
            }
        }
        return null;
    }
}