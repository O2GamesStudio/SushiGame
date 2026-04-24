using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    [SerializeField] private float minimumBootTime = 1.5f;
    [SerializeField] private AppVersionChecker appVersionChecker;

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

        while (elapsed < minimumBootTime)
        {
            elapsed += Time.deltaTime;
            LoadingUI.Instance?.UpdateProgress(Mathf.Clamp01(elapsed / minimumBootTime));
            yield return null;
        }

        LoadingUI.Instance?.UpdateProgress(1f);
        yield return new WaitForSeconds(0.2f);

        bool versionCheckDone = false;
        bool versionPassed = false;

        FirebaseManager.Instance.Initialize(() =>
        {
            appVersionChecker.CheckVersion(() =>
            {
                versionPassed = true;
                versionCheckDone = true;
            }, () =>
            {
                versionCheckDone = true;
            });
        }, error =>
        {
            versionPassed = true;
            versionCheckDone = true;
            LoadingUI.Instance?.ShowError("네트워크 연결을 확인해주세요.");
        });

        yield return new WaitUntil(() => versionCheckDone);

        if (versionPassed)
            operation.allowSceneActivation = true;
    }
}