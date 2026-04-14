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
    public static void LoadGameAsync() => LoadWithCondition(GameScene);
    public static void LoadLobbyAsync() => LoadWithCondition(LobbyScene);

    private static void LoadWithCondition(string sceneName, Func<bool> condition = null)
    {
        // UnityAdsManager.Instance?.ClearAllListeners(); 제거

        if (NetworkChecker.Instance != null)
            NetworkChecker.Instance.Check(() => SpawnRunner(sceneName, condition));
        else
            SpawnRunner(sceneName, condition);
    }

    private static void SpawnRunner(string sceneName, Func<bool> condition)
    {
        var go = new GameObject("SceneLoaderRunner");
        var runner = go.AddComponent<SceneLoaderRunner>();
        runner.Load(sceneName, MinLoadingDuration, condition);
    }
}

public class SceneLoaderRunner : MonoBehaviour
{
    public void Load(string sceneName, float minDuration, Func<bool> condition = null)
    {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(LoadRoutine(sceneName, minDuration, condition));
    }

    private IEnumerator LoadRoutine(string sceneName, float minDuration, Func<bool> condition)
    {
        LoadingUI.Instance?.Show();
        LoadingUI.Instance?.UpdateProgress(0f);

        float startTime = Time.realtimeSinceStartup;
        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            LoadingUI.Instance?.UpdateProgress(op.progress / 0.9f * 0.7f);
            yield return null;
        }

        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            bool conditionMet = condition == null || condition();
            float waitProgress = Mathf.Clamp01(elapsed / minDuration);
            LoadingUI.Instance?.UpdateProgress(0.7f + waitProgress * 0.3f);

            if (elapsed >= minDuration && conditionMet)
                break;
            yield return null;
        }

        LoadingUI.Instance?.UpdateProgress(1f);
        op.allowSceneActivation = true;

        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == sceneName);
        LoadingUI.Instance?.Hide();
        Destroy(gameObject);
    }
}