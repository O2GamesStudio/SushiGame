using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoogleLoginPanel : MonoBehaviour
{
    [SerializeField] private Button loginBtn;
    [SerializeField] private Button closeBtn;
    [SerializeField] TextMeshProUGUI debugText;
    [SerializeField] TextMeshProUGUI debug2Text;

    private Action onLoginSuccess;

    private void Awake()
    {
        loginBtn?.onClick.AddListener(OnLoginBtnClicked);
        closeBtn?.onClick.AddListener(() => gameObject.SetActive(false));
    }

    private void OnDestroy()
    {
        loginBtn?.onClick.RemoveAllListeners();
        closeBtn?.onClick.RemoveAllListeners();
    }

    public void Show(Action onSuccess = null)
    {
        onLoginSuccess = onSuccess;
        gameObject.SetActive(true);
    }

    private void OnLoginBtnClicked()
    {
        debugText?.SetText("[GoogleLogin] 버튼 클릭됨");

        GooglePlayGamesManager.Instance?.LinkToFirebase(
            onSuccess: () =>
            {
                debugText?.SetText("[GoogleLogin] 연동 성공");
                gameObject.SetActive(false);
                onLoginSuccess?.Invoke();
                onLoginSuccess = null;
            },
            onFailed: error => debugText?.SetText($"[GoogleLogin] 실패: {error}"),
            onDebug: msg => debug2Text?.SetText(msg)
        );
    }
}