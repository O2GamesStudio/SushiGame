using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] private GameObject loadingRoot;
    [SerializeField] private Image progressBar;
    [SerializeField] private TextMeshProUGUI errorText;

    public void Show()
    {
        loadingRoot?.SetActive(true);
        if (errorText != null)
            errorText.gameObject.SetActive(false);
    }

    public void Hide()
    {
        loadingRoot?.SetActive(false);
    }

    public void UpdateProgress(float value)
    {
        if (progressBar != null)
            progressBar.fillAmount = Mathf.Clamp01(value);
    }

    public void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.gameObject.SetActive(true);
            errorText.text = message;
        }
    }
}