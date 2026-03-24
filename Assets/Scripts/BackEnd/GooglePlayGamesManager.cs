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

    public void LinkToFirebase(System.Action onSuccess, System.Action<string> onFailed = null)
    {
        StartGoogleSignIn(
            onSuccess: authCode =>
            {
                FirebaseManager.Instance?.LinkWithGoogle(authCode,
                    onSuccess: onSuccess,
                    onFailed: onFailed
                );
            },
            onFailed: onFailed
        );
    }
    public void StartGoogleSignIn(Action<string> onSuccess, Action<string> onFailed = null)
    {
        PlayGamesPlatform.Instance.Authenticate((success) =>
        {
            if (success == SignInStatus.Success)
            {
                PlayGamesPlatform.Instance.RequestServerSideAccess(true, (authCode) =>
                {
                    if (string.IsNullOrEmpty(authCode))
                    {
                        Debug.LogError("[GooglePlayGamesManager] authCode 획득 실패");
                        onFailed?.Invoke("authCode 획득 실패");
                        return;
                    }

                    Debug.Log("[GooglePlayGamesManager] 구글 로그인 성공");
                    onSuccess?.Invoke(authCode);
                });
            }
            else
            {
                Debug.LogError($"[GooglePlayGamesManager] 로그인 실패: {success}");
                onFailed?.Invoke(success.ToString());
            }
        });
    }

    public string GetUserId() => PlayGamesPlatform.Instance.GetUserId();
    public string GetUserName() => PlayGamesPlatform.Instance.GetUserDisplayName();
}