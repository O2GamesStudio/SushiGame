using System;
using System.Threading.Tasks;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine;

public class UserDataService : MonoBehaviour
{
    public static UserDataService Instance { get; private set; }

    private FirebaseFirestore db;
    private const string UsersCollection = "users";

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

    public void Initialize()
    {
        db = FirebaseFirestore.DefaultInstance;
    }

    public void LoadUserData(string userId, Action<UserData> onSuccess, Action<string> onFailed = null)
    {
        Debug.Log($"[UserDataService] 유저 데이터 로드 시작: {userId}");

        db.Collection(UsersCollection).Document(userId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                string error = task.Exception?.Message ?? "Unknown error";
                Debug.LogError($"[UserDataService] 데이터 로드 실패: {error}");
                onFailed?.Invoke(error);
                return;
            }

            var snapshot = task.Result;
            if (snapshot.Exists)
            {
                var data = snapshot.ConvertTo<UserData>();
                Debug.Log($"[UserDataService] 데이터 로드 완료: stage={data.currentStage}");
                onSuccess?.Invoke(data);
            }
            else
            {
                var newData = new UserData { currentStage = 1 };
                Debug.Log($"[UserDataService] 신규 유저 데이터 생성");
                SaveUserData(userId, newData, () => onSuccess?.Invoke(newData));
            }
        });
    }

    public void SaveUserData(string userId, UserData data, Action onSuccess = null, Action<string> onFailed = null)
    {
        db.Collection(UsersCollection).Document(userId).SetAsync(data).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                string error = task.Exception?.Message ?? "Unknown error";
                Debug.LogError($"[UserDataService] 데이터 저장 실패: {error}");
                onFailed?.Invoke(error);
                return;
            }

            onSuccess?.Invoke();
        });
    }

    public void UpdateStage(string userId, int stage, Action onSuccess = null, Action<string> onFailed = null)
    {
        Debug.Log($"[UserDataService] 스테이지 업데이트 시작: {stage}");

        var update = new System.Collections.Generic.Dictionary<string, object>
    {
        { "currentStage", stage }
    };

        db.Collection(UsersCollection).Document(userId).UpdateAsync(update).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                string error = task.Exception?.Message ?? "Unknown error";
                Debug.LogError($"[UserDataService] 스테이지 업데이트 실패: {error}");
                onFailed?.Invoke(error);
                return;
            }

            Debug.Log($"[UserDataService] 스테이지 업데이트 완료: {stage}");
            onSuccess?.Invoke();
        });
    }
}

[FirestoreData]
public class UserData
{
    [FirestoreProperty] public int currentStage { get; set; } = 1;
    [FirestoreProperty] public long lastPlayedAt { get; set; } = 0;
}