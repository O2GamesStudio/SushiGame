using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    public FirebaseAuth Auth { get; private set; }
    public FirebaseUser CurrentUser => Auth?.CurrentUser;
    public bool IsInitialized { get; private set; }
    public bool IsAnonymous => CurrentUser?.IsAnonymous ?? true;

    public event Action OnInitialized;
    public event Action OnLoginSuccess;
    public event Action<string> OnLoginFailed;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Initialize(Action onComplete = null)
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                Auth = FirebaseAuth.DefaultInstance;
                IsInitialized = true;
                OnInitialized?.Invoke();
                onComplete?.Invoke();
            }
            else
            {
                Debug.LogError($"[FirebaseManager] Firebase 초기화 실패: {task.Result}");
            }
        });
    }

    public void SignInAnonymous(Action onSuccess = null, Action<string> onFailed = null)
    {
        if (CurrentUser != null)
        {
            onSuccess?.Invoke();
            return;
        }

        Auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                string error = task.Exception?.Message ?? "Unknown error";
                Debug.LogError($"[FirebaseManager] 익명 로그인 실패: {error}");
                onFailed?.Invoke(error);
                OnLoginFailed?.Invoke(error);
                return;
            }

            Debug.Log($"[FirebaseManager] 익명 로그인 성공 uid:{CurrentUser.UserId}");
            onSuccess?.Invoke();
            OnLoginSuccess?.Invoke();
        });
    }

    public void LinkWithGoogle(string authCode, Action onSuccess = null, Action<string> onFailed = null)
    {
        var credential = Firebase.Auth.PlayGamesAuthProvider.GetCredential(authCode);

        CurrentUser.LinkWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                string error = task.Exception?.Message ?? "Unknown error";
                Debug.LogError($"[FirebaseManager] 구글 연동 실패: {error}");
                onFailed?.Invoke(error);
                return;
            }

            Debug.Log($"[FirebaseManager] 구글 연동 성공 uid:{CurrentUser.UserId}");
            onSuccess?.Invoke();
            OnLoginSuccess?.Invoke();
        });
    }

    public void SignInWithGoogle(string authCode, Action onSuccess = null, Action<string> onFailed = null)
    {
        var credential = Firebase.Auth.PlayGamesAuthProvider.GetCredential(authCode);

        Auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                string error = task.Exception?.Message ?? "Unknown error";
                Debug.LogError($"[FirebaseManager] 구글 로그인 실패: {error}");
                onFailed?.Invoke(error);
                OnLoginFailed?.Invoke(error);
                return;
            }

            Debug.Log($"[FirebaseManager] 구글 로그인 성공 uid:{CurrentUser.UserId}");
            onSuccess?.Invoke();
            OnLoginSuccess?.Invoke();
        });
    }

    public void SignOut()
    {
        Auth.SignOut();
    }
}