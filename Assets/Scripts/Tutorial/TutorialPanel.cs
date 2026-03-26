using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;

public class TutorialPanel : MonoBehaviour
{
    [SerializeField] private Button confirmBtn;
    [SerializeField] private TutorialContent content;

    private Action onConfirm;

    private void OnEnable()
    {
        confirmBtn?.onClick.AddListener(OnConfirmClicked);
        PlayShowAnimation();
    }

    private void OnDisable()
    {
        confirmBtn?.onClick.RemoveAllListeners();
        transform.DOKill();
        content?.StopAnimation();
    }


    public void Show(Action onConfirm = null)
    {
        this.onConfirm = onConfirm;
        gameObject.SetActive(true);
    }

    private void PlayShowAnimation()
    {
        transform.localScale = new Vector3(0f, 1f, 1f);
        transform.DOScaleX(1.15f, 0.2f)
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject)
            .OnComplete(() =>
                transform.DOScaleX(1f, 0.1f)
                    .SetEase(Ease.InQuad)
                    .SetLink(gameObject)
                    .OnComplete(() =>
                        DOVirtual.DelayedCall(1f, () => content?.PlayAnimation())
                            .SetLink(gameObject)));
    }

    private void OnConfirmClicked()
    {
        onConfirm?.Invoke();
        gameObject.SetActive(false);
        TutorialManager.Instance?.HideTutorialParent();
    }
}