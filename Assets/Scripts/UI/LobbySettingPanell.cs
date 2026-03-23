using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class LobbySettingPanel : MonoBehaviour
{
    [SerializeField] private Button exitBtn;
    [SerializeField] private Button googleLinkButton;

    [Header("Toggles")]
    [SerializeField] private Button sfxToggleBtn;
    [SerializeField] private RectTransform sfxHandle;
    [SerializeField] private Button bgmToggleBtn;
    [SerializeField] private RectTransform bgmHandle;
    [SerializeField] private Button vibrationToggleBtn;
    [SerializeField] private RectTransform vibrationHandle;

    private const float OnX = 60f;
    private const float OffX = -60f;
    private const float ToggleDuration = 0.1f;

    private void OnEnable()
    {
        exitBtn?.onClick.AddListener(OnExitClicked);
        googleLinkButton?.onClick.AddListener(OnGoogleLinkClicked);
        sfxToggleBtn?.onClick.AddListener(OnSfxToggled);
        bgmToggleBtn?.onClick.AddListener(OnBgmToggled);

        InitHandle(sfxHandle, SoundManager.Instance.IsSoundEnabled());
        InitHandle(bgmHandle, SoundManager.Instance.IsMusicEnabled());

        UpdateGoogleLinkButton();
    }

    private void OnDisable()
    {
        exitBtn?.onClick.RemoveAllListeners();
        googleLinkButton?.onClick.RemoveAllListeners();
        sfxToggleBtn?.onClick.RemoveAllListeners();
        bgmToggleBtn?.onClick.RemoveAllListeners();
        vibrationToggleBtn?.onClick.RemoveAllListeners();

        sfxHandle?.DOKill();
        bgmHandle?.DOKill();
        vibrationHandle?.DOKill();
    }

    private void InitHandle(RectTransform handle, bool isOn)
    {
        if (handle == null) return;
        handle.anchoredPosition = new Vector2(isOn ? OnX : OffX, handle.anchoredPosition.y);
    }

    private void AnimateHandle(RectTransform handle, bool isOn)
    {
        if (handle == null) return;
        handle.DOKill();
        handle.DOAnchorPosX(isOn ? OnX : OffX, ToggleDuration).SetEase(Ease.OutQuad);
    }

    private void OnSfxToggled()
    {
        bool next = !SoundManager.Instance.IsSoundEnabled();
        SoundManager.Instance.SetSoundEnabled(next);
        AnimateHandle(sfxHandle, next);
    }

    private void OnBgmToggled()
    {
        bool next = !SoundManager.Instance.IsMusicEnabled();
        SoundManager.Instance.SetMusicEnabled(next);
        AnimateHandle(bgmHandle, next);
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