using UnityEngine;
using DG.Tweening;

public class TextFloatingAnimation : MonoBehaviour
{
    [SerializeField] private float floatAmount = 20f;
    [SerializeField] private float duration = 1f;
    [SerializeField] private Ease easeType = Ease.InOutSine;

    private RectTransform rt;
    private Vector2 originPos;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        originPos = rt.anchoredPosition;
    }

    private void OnEnable()
    {
        rt.anchoredPosition = originPos;
        rt.DOAnchorPosY(originPos.y + floatAmount, duration)
            .SetEase(easeType)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
    }

    private void OnDisable()
    {
        rt.DOKill();
        rt.anchoredPosition = originPos;
    }
}