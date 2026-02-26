using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [SerializeField] private LevelData[] levelDataList;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button googleLinkButton;
    [SerializeField] private LoadingUI loadingUI;

    private int currentStage = 1;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Initialize(int stage)
    {
        currentStage = stage;
        UpdateStageUI();

        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(OnStartButtonClicked);

        googleLinkButton.onClick.RemoveAllListeners();
        googleLinkButton.onClick.AddListener(OnGoogleLinkButtonClicked);

        UpdateGoogleLinkButton();
    }

    private void UpdateStageUI()
    {
        if (stageText != null)
            stageText.text = $"{currentStage}층";
    }

    private void UpdateGoogleLinkButton()
    {
        if (googleLinkButton != null)
            googleLinkButton.gameObject.SetActive(FirebaseManager.Instance.IsAnonymous);
    }

    private void OnStartButtonClicked()
    {
        int index = currentStage - 1;
        if (index < 0 || index >= levelDataList.Length) return;

        GameDataTransfer.Instance.SetLevelData(levelDataList[index]);
        SceneLoader.LoadGameAsync(loadingUI);
    }

    private void OnGoogleLinkButtonClicked()
    {
        GooglePlayGamesManager.Instance?.StartGoogleSignIn(idToken =>
        {
            FirebaseManager.Instance.LinkWithGoogle(idToken,
                () => UpdateGoogleLinkButton(),
                null
            );
        });
    }
}