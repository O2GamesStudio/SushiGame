using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System;

public class GooglePlayGamesManager : MonoBehaviour
{
    public static GooglePlayGamesManager Instance { get; private set; }

    public bool IsAuthenticated => PlayGamesPlatform.Instance.IsAuthenticated();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Initialize()
    {
        PlayGamesPlatform.Activate();
    }

    public void LinkToFirebase(Action onSuccess, Action<string> onFailed = null, Action<string> onDebug = null)
    {
        StartGoogleSignIn(
            onSuccess: authCode =>
            {
                onDebug?.Invoke("authCode 획득 성공");
                FirebaseManager.Instance?.LinkWithGoogle(authCode,
                    onSuccess: onSuccess,
                    onFailed: onFailed
                );
            },
            onFailed: onFailed,
            onDebug: onDebug
        );
    }

    public void StartGoogleSignIn(Action<string> onSuccess, Action<string> onFailed = null, Action<string> onDebug = null)
    {
        onDebug?.Invoke("Authenticate 시작");
        PlayGamesPlatform.Instance.Authenticate((success) =>
        {
            onDebug?.Invoke($"SignInStatus: {success}");
            if (success == SignInStatus.Success)
            {
                PlayGamesPlatform.Instance.RequestServerSideAccess(true, (authCode) =>
                {
                    if (string.IsNullOrEmpty(authCode))
                    {
                        onFailed?.Invoke("authCode 획득 실패");
                        return;
                    }
                    onSuccess?.Invoke(authCode);
                });
            }
            else
            {
                onFailed?.Invoke(success.ToString());
            }
        });
    }

    public string GetUserId() => PlayGamesPlatform.Instance.GetUserId();
    public string GetUserName() => PlayGamesPlatform.Instance.GetUserDisplayName();
}