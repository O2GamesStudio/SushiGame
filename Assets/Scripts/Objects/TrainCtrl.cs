using UnityEngine;
using DG.Tweening;

public class TrainCtrl : MonoBehaviour
{
    [Header("Train Movement")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float startX = 12f;
    [SerializeField] private float endX = -12f;
    [SerializeField] private float eventCompleteDelay = 1f;

    [Header("Smoke Float")]
    [SerializeField] private Transform smokeObject;
    [SerializeField] private float floatHeight = 0.3f;
    [SerializeField] private float floatDuration = 0.6f;

    private bool isMoving = false;
    private Tween floatTween;

    private void OnDisable()
    {
        floatTween?.Kill();
    }

    private void Update()
    {
        if (!isMoving) return;

        transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        if (transform.position.x <= endX)
        {
            isMoving = false;
            floatTween?.Kill();
            gameObject.SetActive(false);
        }
    }

    public void PlayOnEventComplete()
    {
        DOVirtual.DelayedCall(eventCompleteDelay, () =>
        {
            transform.position = new Vector3(startX, transform.position.y, transform.position.z);
            isMoving = true;
            gameObject.SetActive(true);
            StartFloating();
        });
    }

    private void StartFloating()
    {
        if (smokeObject == null) return;

        floatTween?.Kill();
        Vector3 originPos = smokeObject.localPosition;

        floatTween = smokeObject
            .DOLocalMoveY(originPos.y + floatHeight, floatDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }
}