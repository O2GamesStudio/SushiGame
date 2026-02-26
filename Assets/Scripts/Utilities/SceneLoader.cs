using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    private const string LobbyScene = "LobbyScene";
    private const string GameScene = "GameScene";

    public static void LoadLobby() => SceneManager.LoadScene(LobbyScene);
    public static void ReloadGame() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    public static void LoadGameAsync(LoadingUI loadingUI = null)
    {
        loadingUI?.Show();
        var op = SceneManager.LoadSceneAsync(GameScene);
        if (loadingUI != null)
            op.completed += _ => loadingUI.Hide();
    }

    public static void LoadLobbyAsync(LoadingUI loadingUI = null)
    {
        loadingUI?.Show();
        var op = SceneManager.LoadSceneAsync(LobbyScene);
        if (loadingUI != null)
            op.completed += _ => loadingUI.Hide();
    }
}