using UnityEngine;
using DG.Tweening;

public class ImageScaleAnimation : MonoBehaviour
{
    [SerializeField] private float scaleAmount = 0.1f;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease easeType = Ease.InOutSine;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        transform.localScale = originalScale;
        transform.DOScale(originalScale * (1f + scaleAmount), duration)
            .SetEase(easeType)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
    }

    private void OnDisable()
    {
        transform.DOKill();
        transform.localScale = originalScale;
    }
}