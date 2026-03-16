using System.Collections;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    private const string LobbyScene = "LobbyScene";
    private const string GameScene = "GameScene";
    private const float MinLoadingDuration = 0.5f;

    public static void LoadLobby(Func<bool> condition = null) => LoadWithCondition(LobbyScene, condition);
    public static void ReloadGame(Func<bool> condition = null) => LoadWithCondition(SceneManager.GetActiveScene().name, condition);
    public static void LoadGameAsync(LoadingUI loadingUI = null) => LoadWithCondition(GameScene, null, loadingUI);
    public static void LoadLobbyAsync(LoadingUI loadingUI = null) => LoadWithCondition(LobbyScene, null, loadingUI);

    private static void LoadWithCondition(string sceneName, Func<bool> condition = null, LoadingUI loadingUI = null)
    {
        UnityAdsManager.Instance?.ClearAllListeners();

        if (NetworkChecker.Instance != null)
        {
            NetworkChecker.Instance.Check(() =>
            {
                var go = new GameObject("SceneLoaderRunner");
                var runner = go.AddComponent<SceneLoaderRunner>();
                runner.Load(sceneName, loadingUI, MinLoadingDuration, condition);
            });
        }
        else
        {
            var go = new GameObject("SceneLoaderRunner");
            var runner = go.AddComponent<SceneLoaderRunner>();
            runner.Load(sceneName, loadingUI, MinLoadingDuration, condition);
        }
    }
}

public class SceneLoaderRunner : MonoBehaviour
{
    public void Load(string sceneName, LoadingUI loadingUI, float minDuration, Func<bool> condition = null)
    {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(LoadRoutine(sceneName, loadingUI, minDuration, condition));
    }

    private IEnumerator LoadRoutine(string sceneName, LoadingUI loadingUI, float minDuration, Func<bool> condition)
    {
        var ui = loadingUI ?? LoadingUI.Instance;
        ui?.Show();
        ui?.UpdateProgress(0f);

        Debug.Log($"[SceneLoader] 씬 로딩 시작: {sceneName}");

        float startTime = Time.realtimeSinceStartup;
        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // 씬 로드: 0 ~ 0.7 구간
        while (op.progress < 0.9f)
        {
            ui?.UpdateProgress(op.progress / 0.9f * 0.7f);
            yield return null;
        }

        // 조건 대기: 0.7 ~ 1.0 구간
        Debug.Log($"[SceneLoader] 씬 로드 완료, 조건 대기 중: {sceneName}");
        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            bool conditionMet = condition == null || condition();

            float waitProgress = Mathf.Clamp01(elapsed / minDuration);
            ui?.UpdateProgress(0.7f + waitProgress * 0.3f);

            if (elapsed >= minDuration && conditionMet)
                break;
            yield return null;
        }

        ui?.UpdateProgress(1f);
        Debug.Log($"[SceneLoader] 씬 전환: {sceneName} / 소요시간: {Time.realtimeSinceStartup - startTime:F2}초");
        op.allowSceneActivation = true;

        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == sceneName);
        ui?.Hide();
        Destroy(gameObject);
    }
}