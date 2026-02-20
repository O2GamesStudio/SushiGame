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

    private Action onComplete;

    private void Awake()
    {
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
        this.onComplete = onComplete;

        leftDoor1.DOLocalMoveX(leftDoor1.localPosition.x - moveDistance, moveDuration)
            .SetEase(easeType);

        leftDoor2.DOLocalMoveX(leftDoor2.localPosition.x - moveDistance, moveDuration)
            .SetEase(easeType);

        rightDoor1.DOLocalMoveX(rightDoor1.localPosition.x + moveDistance, moveDuration)
            .SetEase(easeType);

        rightDoor2.DOLocalMoveX(rightDoor2.localPosition.x + moveDistance, moveDuration)
            .SetEase(easeType)
            .OnComplete(() => onComplete?.Invoke());
    }
}