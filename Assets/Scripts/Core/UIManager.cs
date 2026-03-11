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

    [Header("Setting")]
    [SerializeField] private Button settingButton;
    [SerializeField] private SettingPanel settingPanel;

    private void Awake()
    {
        Instance = this;
        RegisterItemButtons();
        settingButton?.onClick.AddListener(OnSettingClicked);
    }

    private void OnDestroy()
    {
        UnregisterItemButtons();
        settingButton?.onClick.RemoveListener(OnSettingClicked);
    }

    private void RegisterItemButtons()
    {
        randomRemoverButton?.onClick.AddListener(OnRandomRemoverClicked);
        timeFreezerButton?.onClick.AddListener(OnTimeFreezerClicked);
        shufflerButton?.onClick.AddListener(OnShufflerClicked);
        targetRemoverButton?.onClick.AddListener(OnTargetRemoverClicked);
    }

    private void UnregisterItemButtons()
    {
        randomRemoverButton?.onClick.RemoveListener(OnRandomRemoverClicked);
        timeFreezerButton?.onClick.RemoveListener(OnTimeFreezerClicked);
        shufflerButton?.onClick.RemoveListener(OnShufflerClicked);
        targetRemoverButton?.onClick.RemoveListener(OnTargetRemoverClicked);
    }

    private void OnSettingClicked()
    {
        settingPanel?.gameObject.SetActive(true);
    }

    private void OnRandomRemoverClicked() => ItemManager.Instance?.UseRandomSetRemover();
    private void OnTimeFreezerClicked() => ItemManager.Instance?.UseTimeFreezer();
    private void OnShufflerClicked() => ItemManager.Instance?.UseSushiShuffler();
    private void OnTargetRemoverClicked() => ItemManager.Instance?.UseTargetSetRemover();

    public void ShowWin() => gameUI?.ShowWin();
    public void ShowLose() => gameUI?.ShowLose();
    public void ShowGame() => gameUI?.ShowGame();
}