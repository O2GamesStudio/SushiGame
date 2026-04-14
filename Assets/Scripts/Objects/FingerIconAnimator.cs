using UnityEngine;
using DG.Tweening;

public class FingerIconAnimator : MonoBehaviour
{
    [SerializeField] private float moveDuration = 1.2f;
    [SerializeField] private float idleDelay = 0.5f;

    private Sequence sequence;

    public void PlayMoveLoop(Vector3 from, Vector3 to)
    {
        transform.position = from;
        gameObject.SetActive(true);

        sequence?.Kill();
        sequence = DOTween.Sequence()
            .AppendInterval(idleDelay)
            .Append(transform.DOMove(to, moveDuration).SetEase(Ease.InOutSine))
            .AppendInterval(0.3f)
            .AppendCallback(() => transform.position = from)
            .SetLoops(-1)
            .SetLink(gameObject);
    }

    public void Hide()
    {
        sequence?.Kill();
        sequence = null;
        gameObject.SetActive(false);
    }
}