using UnityEngine;
using TMPro;

public class GameUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private Color frozenColor = Color.cyan;

    [Header("Progress Display")]
    [SerializeField] private TextMeshProUGUI progressText;

    private Color normalColor = Color.white;

    public void UpdateTimer(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60);
        int secs = Mathf.FloorToInt(seconds % 60);
        timerText.text = $"{minutes:00}:{secs:00}";
    }

    public void SetTimerFrozen(bool isFrozen)
    {
        timerText.color = isFrozen ? frozenColor : normalColor;
    }

    public void ShowGame()
    {
        winPanel.SetActive(false);
        losePanel.SetActive(false);
    }

    public void UpdateProgress(int current, int total)
    {
        if (progressText != null)
            progressText.text = $"{current}/{total}";
    }

    public void ShowWin() => winPanel.SetActive(true);
    public void ShowLose() => losePanel.SetActive(true);
}