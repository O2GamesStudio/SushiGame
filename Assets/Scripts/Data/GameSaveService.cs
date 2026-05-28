using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using Newtonsoft.Json;
using System.Collections.Generic;

public class GameSaveService : MonoBehaviour
{
    public static GameSaveService Instance { get; private set; }

    private const string LegacyLocalSaveKey = "GameSaveData";
    private const string SaveFileName = "gamesave.json";
    private const string FirestoreCollection = "gameSaves";

    private string _savePath;
    private readonly object _fileLock = new object();
    private int mergeCountSinceLastFirebaseSave = 0;
    private const int FirebaseSaveInterval = 5;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _savePath = Path.Combine(Application.persistentDataPath, SaveFileName);
        MigrateLegacyData();
    }

    // 기존 PlayerPrefs 데이터를 파일로 마이그레이션 (업데이트 후 최초 1회)
    private void MigrateLegacyData()
    {
        if (File.Exists(_savePath)) return;
        if (!PlayerPrefs.HasKey(LegacyLocalSaveKey)) return;

        try
        {
            string json = PlayerPrefs.GetString(LegacyLocalSaveKey);
            File.WriteAllText(_savePath, json);
            PlayerPrefs.DeleteKey(LegacyLocalSaveKey);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameSaveService] 마이그레이션 실패: {e.Message}");
        }
    }

    public bool HasSaveData() => File.Exists(_savePath) || PlayerPrefs.HasKey(LegacyLocalSaveKey);

    // 동기 저장: OnApplicationPause 전용 (앱 kill 전 저장 보장)
    public void SaveLocal(GameSaveData data)
    {
        try
        {
            data.savedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string json = JsonConvert.SerializeObject(data);
            lock (_fileLock)
            {
                File.WriteAllText(_savePath, json);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameSaveService] 로컬 저장 실패: {e.Message}");
        }
    }

    // 비동기 저장: OnMerged 전용 (메인 스레드 블로킹 없음)
    public void SaveLocalAsync(GameSaveData data)
    {
        try
        {
            data.savedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string json = JsonConvert.SerializeObject(data);
            string path = _savePath;
            Task.Run(() =>
            {
                lock (_fileLock)
                {
                    try { File.WriteAllText(path, json); }
                    catch (Exception e) { Debug.LogError($"[GameSaveService] 비동기 저장 실패: {e.Message}"); }
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameSaveService] 비동기 저장 시작 실패: {e.Message}");
        }
    }

    public void ClearSave(int stageIndex)
    {
        ClearLocal();
        ClearFirestore();
    }

    public GameSaveData LoadLocal()
    {
        if (File.Exists(_savePath))
        {
            try
            {
                string json = File.ReadAllText(_savePath);
                return JsonConvert.DeserializeObject<GameSaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameSaveService] 파일 로드 실패: {e.Message}");
                ClearLocal();
            }
        }

        // PlayerPrefs 폴백 (마이그레이션이 실패한 경우)
        if (PlayerPrefs.HasKey(LegacyLocalSaveKey))
        {
            try
            {
                return JsonConvert.DeserializeObject<GameSaveData>(PlayerPrefs.GetString(LegacyLocalSaveKey));
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameSaveService] PlayerPrefs 폴백 로드 실패: {e.Message}");
                PlayerPrefs.DeleteKey(LegacyLocalSaveKey);
            }
        }

        return null;
    }

    public void ClearLocal()
    {
        try
        {
            if (File.Exists(_savePath))
                File.Delete(_savePath);

            if (PlayerPrefs.HasKey(LegacyLocalSaveKey))
            {
                PlayerPrefs.DeleteKey(LegacyLocalSaveKey);
                PlayerPrefs.Save();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameSaveService] 로컬 삭제 실패: {e.Message}");
        }
    }

    public void OnMerged(GameSaveData data)
    {
        SaveLocalAsync(data);
        mergeCountSinceLastFirebaseSave++;
        if (mergeCountSinceLastFirebaseSave >= FirebaseSaveInterval)
        {
            mergeCountSinceLastFirebaseSave = 0;
            SaveToFirestore(data);
        }
    }

    public void SaveToFirestore(GameSaveData data)
    {
        if (FirebaseManager.Instance?.IsAnonymous ?? true) return;
        string userId = FirebaseManager.Instance.CurrentUser.UserId;

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
