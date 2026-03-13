using UnityEngine;
using DG.Tweening;

public class ImageRotateAnimation : MonoBehaviour
{
    [SerializeField] private float rotateDegree = 360f;
    [SerializeField] private float duration = 2f;
    [SerializeField] private Ease easeType = Ease.Linear;
    [SerializeField] private LoopType loopType = LoopType.Restart;

    private RectTransform rt;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        rt.DOKill();
        rt.DORotate(new Vector3(0f, 0f, rotateDegree), duration, RotateMode.LocalAxisAdd)
            .SetEase(easeType)
            .SetLoops(-1, loopType)
            .SetLink(gameObject);
    }

    private void OnDisable()
    {
        rt.DOKill();
        rt.localRotation = Quaternion.identity;
    }
}