using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoogleLoginPanel : MonoBehaviour
{
    [SerializeField] private Button loginBtn;
    [SerializeField] private Button closeBtn;
    [SerializeField] TextMeshProUGUI debugText;

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

        if (GooglePlayGamesManager.Instance == null)
        {
            debugText?.SetText("[GoogleLogin] GooglePlayGamesManager.Instance == null");
            return;
        }

        debugText?.SetText("[GoogleLogin] StartGoogleSignIn 호출");

        GooglePlayGamesManager.Instance.StartGoogleSignIn(
            onSuccess: authCode =>
            {
                debugText?.SetText($"[GoogleLogin] authCode 획득 성공: {authCode?.Substring(0, Mathf.Min(10, authCode.Length))}...");

                FirebaseManager.Instance?.LinkWithGoogle(authCode,
                    onSuccess: () =>
                    {
                        debugText?.SetText("[GoogleLogin] Firebase 연동 성공");
                        gameObject.SetActive(false);
                        onLoginSuccess?.Invoke();
                        onLoginSuccess = null;
                    },
                    onFailed: error =>
                    {
                        debugText?.SetText($"[GoogleLogin] Firebase 연동 실패: {error}");
                    }
                );
            },
            onFailed: error =>
            {
                debugText?.SetText($"[GoogleLogin] 구글 로그인 실패: {error}");
            }
        );
    }
}