using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class NetworkChecker : MonoBehaviour
{
    public static NetworkChecker Instance { get; private set; }

    [SerializeField] private GameObject noInternetPanel;
    [SerializeField] private Button retryButton;

    private System.Action onConnected;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        retryButton?.onClick.AddListener(OnRetryClicked);
    }

    public bool IsConnected => Application.internetReachability != NetworkReachability.NotReachable;

    public void Check(System.Action onSuccess, System.Action onFail = null)
    {
        if (IsConnected)
        {
            noInternetPanel?.SetActive(false);
            onSuccess?.Invoke();
        }
        else
        {
            onConnected = onSuccess;
            noInternetPanel?.SetActive(true);
            onFail?.Invoke();
        }
    }

    public void CheckWithPing(System.Action onSuccess, System.Action onFail = null)
    {
        StartCoroutine(PingCoroutine(onSuccess, onFail));
    }

    private IEnumerator PingCoroutine(System.Action onSuccess, System.Action onFail)
    {
        var ping = new Ping("8.8.8.8");
        float timeout = 3f;
        float elapsed = 0f;

        while (!ping.isDone && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        bool connected = ping.isDone && ping.time >= 0;

        if (connected)
        {
            noInternetPanel?.SetActive(false);
            onSuccess?.Invoke();
        }
        else
        {
            onConnected = onSuccess;
            noInternetPanel?.SetActive(true);
            onFail?.Invoke();
        }
    }

    private void OnRetryClicked()
    {
        CheckWithPing(() =>
        {
            noInternetPanel?.SetActive(false);
            onConnected?.Invoke();
            onConnected = null;
        });
    }
}