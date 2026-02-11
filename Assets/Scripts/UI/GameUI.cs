using UnityEngine;
using TMPro;

public class GameUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private WinPanel winPanel;
    [SerializeField] private LosePanel losePanel;
    [SerializeField] private Color frozenColor = Color.cyan;

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
        winPanel.gameObject.SetActive(false);
        losePanel.gameObject.SetActive(false);
    }

    public void ShowWin()
    {
        winPanel.gameObject.SetActive(true);
    }

    public void ShowLose()
    {
        losePanel.gameObject.SetActive(true);
    }
}