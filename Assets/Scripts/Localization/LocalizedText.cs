using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string key;
    private TMP_Text textComponent;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (LocalizationManager.Instance == null || string.IsNullOrEmpty(key)) return;
        textComponent.text = LocalizationManager.Instance.Get(key);
    }

    public void SetKey(string newKey)
    {
        key = newKey;
        Refresh();
    }
}