using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    [SerializeField] private float minimumBootTime = 1.5f;

    private void Start()
    {
        LoadingUI.Instance?.Show();
        StartCoroutine(BootRoutine());
    }

    private IEnumerator BootRoutine()
    {
        float elapsed = 0f;
        var operation = SceneManager.LoadSceneAsync("LobbyScene");
        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            elapsed += Time.deltaTime;
            LoadingUI.Instance?.UpdateProgress(operation.progress / 0.9f);
            yield return null;
        }

        // 최소 부팅 시간 보장
        while (elapsed < minimumBootTime)
        {
            elapsed += Time.deltaTime;
            LoadingUI.Instance?.UpdateProgress(Mathf.Clamp01(elapsed / minimumBootTime));
            yield return null;
        }

        LoadingUI.Instance?.UpdateProgress(1f);
        yield return new WaitForSeconds(0.2f);

        operation.allowSceneActivation = true;
    }
}