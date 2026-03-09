using UnityEngine;
using TMPro;
using DG.Tweening;

public class GameUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private Color frozenColor = Color.cyan;

    [Header("Progress Display")]
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Timer Warning")]
    [SerializeField] private float warningThreshold = 30f;
    [SerializeField] private Color warningTextColor = Color.red;

    private Color normalColor;
    private bool isWarningActive = false;
    private Tween pulseTween;

    private void Awake()
    {
        if (timerText != null)
            normalColor = timerText.color;
    }

    public void UpdateTimer(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60);
        int secs = Mathf.FloorToInt(seconds % 60);
        timerText.text = $"{minutes:00}:{secs:00}";

        if (seconds <= warningThreshold && !isWarningActive)
            StartWarning();
        else if (seconds > warningThreshold && isWarningActive)
            StopWarning();
    }

    private void StartWarning()
    {
        isWarningActive = true;

        timerText.color = warningTextColor;

        pulseTween = timerText.transform
            .DOScale(Vector3.one * 1.15f, 0.4f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopWarning()
    {
        isWarningActive = false;

        pulseTween?.Kill();

        timerText.transform.localScale = Vector3.one;
        timerText.color = normalColor;
    }

    public void UpdateStage(int stage)
    {
        if (stageText != null)
            stageText.text = $"{stage}층";
    }

    public void SetTimerFrozen(bool isFrozen)
    {
        if (isFrozen)
        {
            StopWarning();
            timerText.color = frozenColor;
        }
        else
        {
            timerText.color = normalColor;
        }
    }

    public void SetTimerText(string text)
    {
        if (timerText != null)
        {
            StopWarning();
            timerText.text = text;
        }
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

    public void ShowWin()
    {
        StopWarning();
        winPanel.SetActive(true);
    }

    public void ShowLose()
    {
        StopWarning();
        losePanel.SetActive(true);
    }

    private void OnDestroy()
    {
        pulseTween?.Kill();
    }
}