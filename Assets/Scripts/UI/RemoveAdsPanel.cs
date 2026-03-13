using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class RemoveAdsPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelObject;
    [SerializeField] private Button exitBtn;

    private void OnEnable()
    {
        exitBtn?.onClick.AddListener(OnExitClicked);
        PlayShowAnimation();
    }

    private void OnDisable()
    {
        exitBtn?.onClick.RemoveAllListeners();
        panelObject.transform.DOKill();
    }

    private void PlayShowAnimation()
    {
        panelObject.transform.localScale = Vector3.zero;
        panelObject.transform.DOScale(1.2f, 0.2f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
                panelObject.transform.DOScale(1f, 0.1f).SetEase(Ease.InQuad));
    }

    private void OnExitClicked() => gameObject.SetActive(false);
}