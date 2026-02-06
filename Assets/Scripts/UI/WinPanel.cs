using UnityEngine;
using UnityEngine.UI;

public class WinPanel : MonoBehaviour
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