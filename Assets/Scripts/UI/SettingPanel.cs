using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : MonoBehaviour
{
    [SerializeField] private Button exitBtn;
    [SerializeField] private Button lobbyBtn;
    [SerializeField] private Button retryBtn;

    private void OnEnable()
    {
        exitBtn?.onClick.AddListener(OnExitClicked);
        lobbyBtn?.onClick.AddListener(OnLobbyClicked);
        retryBtn?.onClick.AddListener(OnRetryClicked);
        GameManager.Instance?.FreezeTimer(float.MaxValue);
    }

    private void OnDisable()
    {
        exitBtn?.onClick.RemoveAllListeners();
        lobbyBtn?.onClick.RemoveAllListeners();
        retryBtn?.onClick.RemoveAllListeners();
        GameManager.Instance?.ResumeTimer();
    }

    private void OnExitClicked()
    {
        gameObject.SetActive(false);
    }

    private void OnLobbyClicked()
    {
        SceneLoader.LoadLobby();
    }

    private void OnRetryClicked()
    {
        SceneLoader.LoadGameAsync(LoadingUI.Instance);
    }
}