using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class StoreManager : MonoBehaviour
{
    [SerializeField] private Button toLobbyBtn;
    [SerializeField] private float moveAmount = 15f;
    [SerializeField] private float duration = 0.4f;

    private RectTransform lobbyBtnRT;
    private Vector2 originalPos;

    private void Awake()
    {
        lobbyBtnRT = toLobbyBtn.GetComponent<RectTransform>();
        originalPos = lobbyBtnRT.anchoredPosition;
    }

    private void OnEnable()
    {
        PlayLobbyBtnAnimation();
    }

    private void OnDisable()
    {
        lobbyBtnRT.DOKill();
        lobbyBtnRT.anchoredPosition = originalPos;
    }

    private void PlayLobbyBtnAnimation()
    {
        lobbyBtnRT.DOAnchorPosX(originalPos.x + moveAmount, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(toLobbyBtn.gameObject);
    }
}