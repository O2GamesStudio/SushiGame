using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HiddenSushiTutorialContent : TutorialContent
{
    [SerializeField] private Image hiddenSushiImage;

    public override void PlayAnimation()
    {
        var rt = hiddenSushiImage.rectTransform;
        Vector2 originPos = rt.anchoredPosition;

        PlayLoop(rt, originPos);
    }

    private void PlayLoop(RectTransform rt, Vector2 originPos)
    {
        rt.DOAnchorPosY(originPos.y + 140f, 0.5f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
                rt.DOShakeAnchorPos(0.4f, new Vector2(15f, 0f), 20, 90f)
                    .OnComplete(() =>
                        rt.DOAnchorPosY(originPos.y, 0.4f)
                            .SetEase(Ease.InQuad)
                            .OnComplete(() =>
                                DOVirtual.DelayedCall(0.3f, () => PlayLoop(rt, originPos)))));
    }

    public override void StopAnimation()
    {
        hiddenSushiImage.rectTransform.DOKill();
    }
}