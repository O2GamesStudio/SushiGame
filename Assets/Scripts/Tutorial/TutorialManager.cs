using UnityEngine;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [System.Serializable]
    private struct TutorialEntry
    {
        public int triggerStage;
        public TutorialPanel panel;
    }

    [SerializeField] private GameObject tutorialParent;
    [SerializeField] private List<TutorialEntry> tutorials;

    private void Awake() => Instance = this;

    public void TryShowTutorial(int stage, System.Action onComplete = null)
    {
        var entry = tutorials.Find(t => t.triggerStage == stage);
        if (entry.panel == null)
        {
            onComplete?.Invoke();
            return;
        }

        tutorialParent?.SetActive(true);
        entry.panel.Show(() =>
        {
            tutorialParent?.SetActive(false);
            onComplete?.Invoke();
        });
    }

    public void HideTutorialParent()
    {
        tutorialParent?.SetActive(false);
    }
}