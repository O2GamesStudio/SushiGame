using UnityEngine;
using UnityEngine.UI;

public class LosePanel : MonoBehaviour
{
    [SerializeField] private Button restartButton;

    private void Start()
    {
        restartButton.onClick.AddListener(OnRestartButton);
    }

    private void OnRestartButton()
    {
        GameManager.Instance.RestartGame();
    }
}