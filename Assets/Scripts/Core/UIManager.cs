using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private GameUI gameUI;

    [Header("Item Buttons")]
    [SerializeField] private Button randomRemoverButton;
    [SerializeField] private Button timeFreezerButton;
    [SerializeField] private Button shufflerButton;
    [SerializeField] private Button targetRemoverButton;

    private void Awake()
    {
        Instance = this;
        RegisterItemButtons();
    }

    private void OnDestroy()
    {
        UnregisterItemButtons();
    }

    private void RegisterItemButtons()
    {
        if (randomRemoverButton != null)
            randomRemoverButton.onClick.AddListener(OnRandomRemoverClicked);

        if (timeFreezerButton != null)
            timeFreezerButton.onClick.AddListener(OnTimeFreezerClicked);

        if (shufflerButton != null)
            shufflerButton.onClick.AddListener(OnShufflerClicked);

        if (targetRemoverButton != null)
            targetRemoverButton.onClick.AddListener(OnTargetRemoverClicked);
    }

    private void UnregisterItemButtons()
    {
        if (randomRemoverButton != null)
            randomRemoverButton.onClick.RemoveListener(OnRandomRemoverClicked);

        if (timeFreezerButton != null)
            timeFreezerButton.onClick.RemoveListener(OnTimeFreezerClicked);

        if (shufflerButton != null)
            shufflerButton.onClick.RemoveListener(OnShufflerClicked);

        if (targetRemoverButton != null)
            targetRemoverButton.onClick.RemoveListener(OnTargetRemoverClicked);
    }

    private void OnRandomRemoverClicked()
    {
        ItemManager.Instance?.UseRandomSetRemover();
    }

    private void OnTimeFreezerClicked()
    {
        ItemManager.Instance?.UseTimeFreezer();
    }

    private void OnShufflerClicked()
    {
        ItemManager.Instance?.UseSushiShuffler();
    }

    private void OnTargetRemoverClicked()
    {
        ItemManager.Instance?.UseTargetSetRemover();
    }

    public void ShowWin()
    {
        gameUI?.ShowWin();
    }

    public void ShowLose()
    {
        gameUI?.ShowLose();
    }

    public void ShowGame()
    {
        gameUI?.ShowGame();
    }
}