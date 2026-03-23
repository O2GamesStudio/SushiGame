using System;
using UnityEngine;
using UnityEngine.UI;

public class GoogleLoginPanel : MonoBehaviour
{
    [SerializeField] private Button loginBtn;
    [SerializeField] private Button closeBtn;

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
        GooglePlayGamesManager.Instance?.StartGoogleSignIn(
            onSuccess: authCode =>
            {
                FirebaseManager.Instance?.LinkWithGoogle(authCode,
                    onSuccess: () =>
                    {
                        gameObject.SetActive(false);
                        onLoginSuccess?.Invoke();
                        onLoginSuccess = null;
                    },
                    onFailed: error => Debug.LogError($"[GoogleLoginPanel] Firebase 연동 실패: {error}")
                );
            },
            onFailed: error => Debug.LogError($"[GoogleLoginPanel] 구글 로그인 실패: {error}")
        );
    }
}