using UnityEngine;
using UnityEngine.UI;

public class LobbySettingPanel : MonoBehaviour
{
    [SerializeField] private Button exitBtn;
    [SerializeField] private Button googleLinkButton;

    private void OnEnable()
    {
        exitBtn?.onClick.AddListener(OnExitClicked);
        googleLinkButton?.onClick.AddListener(OnGoogleLinkClicked);
        UpdateGoogleLinkButton();
    }

    private void OnDisable()
    {
        exitBtn?.onClick.RemoveAllListeners();
        googleLinkButton?.onClick.RemoveAllListeners();
    }

    private void OnExitClicked() => gameObject.SetActive(false);

    private void OnGoogleLinkClicked()
    {
        GooglePlayGamesManager.Instance?.StartGoogleSignIn(idToken =>
        {
            FirebaseManager.Instance.LinkWithGoogle(idToken,
                () => UpdateGoogleLinkButton(),
                null
            );
        });
    }

    private void UpdateGoogleLinkButton()
    {
        if (googleLinkButton != null)
            googleLinkButton.gameObject.SetActive(FirebaseManager.Instance.IsAnonymous);
    }
}