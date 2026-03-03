using UnityEngine;
using DG.Tweening;
using System;

public class DoorTransition : MonoBehaviour
{
    [SerializeField] private Transform leftDoor1;
    [SerializeField] private Transform leftDoor2;
    [SerializeField] private Transform rightDoor1;
    [SerializeField] private Transform rightDoor2;

    [Header("Animation Settings")]
    [SerializeField] private float moveDuration = 0.8f;
    [SerializeField] private Ease easeType = Ease.OutBack;

    private float doorWidth = 330f;
    private float moveDistance;
    private Vector3 leftDoor1Origin, leftDoor2Origin, rightDoor1Origin, rightDoor2Origin;

    private void Awake()
    {
        leftDoor1Origin = leftDoor1.localPosition;
        leftDoor2Origin = leftDoor2.localPosition;
        rightDoor1Origin = rightDoor1.localPosition;
        rightDoor2Origin = rightDoor2.localPosition;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            float screenHalfWidth = canvasRect.rect.width / 2f;
            moveDistance = screenHalfWidth + doorWidth * 2f + 100f;
        }
        else
        {
            moveDistance = 1500f;
        }
    }

    public void PlayOpenAnimation(Action onComplete = null)
    {
        leftDoor1.DOKill();
        leftDoor2.DOKill();
        rightDoor1.DOKill();
        rightDoor2.DOKill();

        leftDoor1.DOLocalMoveX(leftDoor1Origin.x - moveDistance, moveDuration).SetEase(easeType);
        leftDoor2.DOLocalMoveX(leftDoor2Origin.x - moveDistance, moveDuration).SetEase(easeType);
        rightDoor1.DOLocalMoveX(rightDoor1Origin.x + moveDistance, moveDuration).SetEase(easeType);
        rightDoor2.DOLocalMoveX(rightDoor2Origin.x + moveDistance, moveDuration).SetEase(easeType)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void PlayCloseAnimation(Action onComplete = null)
    {
        leftDoor1.DOKill();
        leftDoor2.DOKill();
        rightDoor1.DOKill();
        rightDoor2.DOKill();

        leftDoor1.DOLocalMoveX(leftDoor1Origin.x, moveDuration).SetEase(easeType);
        leftDoor2.DOLocalMoveX(leftDoor2Origin.x, moveDuration).SetEase(easeType);
        rightDoor1.DOLocalMoveX(rightDoor1Origin.x, moveDuration).SetEase(easeType);
        rightDoor2.DOLocalMoveX(rightDoor2Origin.x, moveDuration).SetEase(easeType)
            .OnComplete(() => onComplete?.Invoke());
    }
}