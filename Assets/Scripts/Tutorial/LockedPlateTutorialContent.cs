using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class LockedPlateTutorialContent : TutorialContent
{
    [SerializeField] private Image plateLid;

    public override void PlayAnimation()
    {
        var rt = plateLid.rectTransform;
        Vector2 originPos = rt.anchoredPosition;

        rt.DOAnchorPosY(originPos.y + 140f, 0.6f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(plateLid.gameObject);
    }

    public override void StopAnimation()
    {
        plateLid.rectTransform.DOKill();
    }
}