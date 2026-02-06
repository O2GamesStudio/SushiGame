using UnityEngine;
using DG.Tweening;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlateManager plateManager;

    private Plate selectedPlate;
    private Sushi draggedSushi;
    private Vector3 originalPosition;

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
        var hit = GetPlateAtMousePosition();
        if (hit != null && hit.ActiveCount > 0)
        {
            selectedPlate = hit;
            draggedSushi = selectedPlate.PeekTop();
            originalPosition = draggedSushi.transform.position;
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

        if (targetPlate != null && targetPlate != selectedPlate)
        {
            if (plateManager.CanMoveSushi(selectedPlate, targetPlate))
            {
                plateManager.MoveSushi(selectedPlate, targetPlate);
            }
            else
            {
                ReturnToOriginalPosition();
            }
        }
        else
        {
            ReturnToOriginalPosition();
        }

        draggedSushi = null;
        selectedPlate = null;
    }

    private void ReturnToOriginalPosition()
    {
        draggedSushi.transform.DOMove(originalPosition, 0.2f).SetEase(Ease.OutQuad);
    }

    private Plate GetPlateAtMousePosition()
    {
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        var hit = Physics2D.Raycast(ray.origin, ray.direction);

        if (hit.collider != null)
        {
            return hit.collider.GetComponent<Plate>();
        }

        return null;
    }
}
