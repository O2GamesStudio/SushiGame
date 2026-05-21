using System;
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
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Initialize(Action onComplete = null, Action<string> onFailed = null)
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
                Debug.LogError($"[FirebaseManager] 초기화 실패: {task.Result}");
                onFailed?.Invoke(task.Result.ToString());
            }
        });
    }

    private const int MaxAnonymousRetry = 3;

    public void SignInAnonymous(Action onSuccess = null, Action<string> onFailed = null, int retryCount = 0)
    {
        if (CurrentUser != null) { onSuccess?.Invoke(); return; }

        Auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                if (retryCount < MaxAnonymousRetry)
                {
                    SignInAnonymous(onSuccess, onFailed, retryCount + 1);
                    return;
                }
                string error = task.Exception?.Message ?? "Unknown error";
                Debug.LogError($"[FirebaseManager] 익명 로그인 실패: {error}");
                onFailed?.Invoke(error);
                OnLoginFailed?.Invoke(error);
                return;
            }
            onSuccess?.Invoke();
            OnLoginSuccess?.Invoke();
        });
    }

    public void LinkWithGoogle(string authCode, Action onSuccess = null, Action<string> onFailed = null)
    {
        var credential = PlayGamesAuthProvider.GetCredential(authCode);

        if (CurrentUser == null)
        {
            Debug.Log("[FirebaseManager] CurrentUser null → SignInWithGoogle");
            SignInWithGoogle(authCode, onSuccess, onFailed);
            return;
        }

        Debug.Log($"[FirebaseManager] LinkWithCredential 시작 / IsAnonymous: {CurrentUser.IsAnonymous}");
        CurrentUser.LinkWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
        {
            Debug.Log($"[FirebaseManager] Link 결과 - Faulted:{task.IsFaulted} Canceled:{task.IsCanceled}");
            if (task.IsCanceled || task.IsFaulted)
            {
                var baseEx = task.Exception?.GetBaseException();
                var firebaseEx = baseEx as FirebaseException;

                Debug.LogError($"[FirebaseManager] Link 예외: {baseEx?.Message} / ErrorCode: {firebaseEx?.ErrorCode}");

                bool isCredentialInUse = firebaseEx?.ErrorCode == (int)AuthError.CredentialAlreadyInUse
                    || (baseEx?.Message?.Contains("already associated") ?? false);

                if (isCredentialInUse)
                {
                    Debug.Log("[FirebaseManager] CredentialAlreadyInUse → SignInWithGoogle fallback");
                    SignInWithGoogle(authCode, onSuccess, onFailed);
                    return;
                }

                string error = baseEx?.Message ?? "Unknown error";
                onFailed?.Invoke(error);
                return;
            }
            Debug.Log("[FirebaseManager] Link 성공");
            onSuccess?.Invoke();
            OnLoginSuccess?.Invoke();
        });
    }

    public void SignInWithGoogle(string authCode, Action onSuccess = null, Action<string> onFailed = null)
    {
        var credential = PlayGamesAuthProvider.GetCredential(authCode);

        Debug.Log("[FirebaseManager] SignInWithCredential 시작");
        Auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
        {
            Debug.Log($"[FirebaseManager] SignIn 결과 - Faulted:{task.IsFaulted} Canceled:{task.IsCanceled}");
            if (task.IsCanceled || task.IsFaulted)
            {
                string error = task.Exception?.GetBaseException()?.Message ?? "Unknown error";
                Debug.LogError($"[FirebaseManager] SignIn 예외: {error}");
                onFailed?.Invoke(error);
                OnLoginFailed?.Invoke(error);
                return;
            }
            Debug.Log("[FirebaseManager] SignIn 성공");
            onSuccess?.Invoke();
            OnLoginSuccess?.Invoke();
        });
    }

    public void SignOut() => Auth.SignOut();
}