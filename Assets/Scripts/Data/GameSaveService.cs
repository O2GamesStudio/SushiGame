using System;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using Newtonsoft.Json;
using System.Collections.Generic;

public class GameSaveService : MonoBehaviour
{
    public static GameSaveService Instance { get; private set; }

    private const string LocalSaveKey = "GameSaveData";
    private const string FirestoreCollection = "gameSaves";
    private int mergeCountSinceLastFirebaseSave = 0;
    private const int FirebaseSaveInterval = 5;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool HasSaveData() => PlayerPrefs.HasKey(LocalSaveKey);

    public void SaveLocal(GameSaveData data)
    {
        try
        {
            data.savedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            PlayerPrefs.SetString(LocalSaveKey, JsonConvert.SerializeObject(data));
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameSaveService] 로컬 저장 실패: {e.Message}");
        }
    }

    public GameSaveData LoadLocal()
    {
        if (!HasSaveData()) return null;

        try
        {
            return JsonConvert.DeserializeObject<GameSaveData>(PlayerPrefs.GetString(LocalSaveKey));
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameSaveService] 로컬 로드 실패: {e.Message}");
            ClearLocal();
            return null;
        }
    }

    public void ClearLocal()
    {
        PlayerPrefs.DeleteKey(LocalSaveKey);
        PlayerPrefs.Save();
    }

    public void OnMerged(GameSaveData data)
    {
        SaveLocal(data);
        mergeCountSinceLastFirebaseSave++;
        if (mergeCountSinceLastFirebaseSave >= FirebaseSaveInterval)
        {
            mergeCountSinceLastFirebaseSave = 0;
            SaveToFirestore(data);
        }
    }

    public void SaveToFirestore(GameSaveData data)
    {
        string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId)) return;

        try
        {
            data.savedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string json = JsonConvert.SerializeObject(data);
            var doc = new Dictionary<string, object> { { "data", json } };

            FirebaseFirestore.DefaultInstance
                .Collection(FirestoreCollection)
                .Document(userId)
                .SetAsync(doc);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameSaveService] Firestore 저장 실패: {e.Message}");
        }
    }



    public void LoadFromFirestore(Action<GameSaveData> onSuccess, Action onFailed = null)
    {
        string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId)) { onFailed?.Invoke(); return; }

        FirebaseFirestore.DefaultInstance
            .Collection(FirestoreCollection)
            .Document(userId)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled || !task.Result.Exists)
                {
                    onFailed?.Invoke();
                    return;
                }

                try
                {
                    string json = task.Result.GetValue<string>("data");
                    onSuccess?.Invoke(JsonConvert.DeserializeObject<GameSaveData>(json));
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GameSaveService] Firestore 로드 실패: {e.Message}");
                    onFailed?.Invoke();
                }
            });
    }

    public void ClearFirestore()
    {
        string userId = FirebaseManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId)) return;

        FirebaseFirestore.DefaultInstance
            .Collection(FirestoreCollection)
            .Document(userId)
            .DeleteAsync();
    }

    public GameSaveData ResolveConflict(GameSaveData local, GameSaveData firebase)
    {
        if (local == null) return firebase;
        if (firebase == null) return local;
        return local.savedTimestamp >= firebase.savedTimestamp ? local : firebase;
    }
}