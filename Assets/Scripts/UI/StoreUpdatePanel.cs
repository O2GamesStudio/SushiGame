using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoreUpdatePanel : MonoBehaviour
{
    [SerializeField] private Button updateBtn;
    [SerializeField] private TextMeshProUGUI messageText;

    private string storeUrl;

    private void OnEnable()
    {
        updateBtn?.onClick.AddListener(OnUpdateBtnClicked);
    }

    private void OnDisable()
    {
        updateBtn?.onClick.RemoveAllListeners();
    }

    public void Show(string url, string message = null)
    {
        storeUrl = url;
        if (messageText != null && !string.IsNullOrEmpty(message))
            messageText.text = message;
        gameObject.SetActive(true);
    }

    private void OnUpdateBtnClicked()
    {
        if (!string.IsNullOrEmpty(storeUrl))
            Application.OpenURL(storeUrl);
    }
}