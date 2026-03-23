using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("AudioSource Pool Settings")]
    [SerializeField] private int initialPoolSize = 10;
    [SerializeField] private int maxPoolSize = 20;

    [Header("BGM Clips")]
    [SerializeField] private AudioClip lobbyBGM;
    [SerializeField] private AudioClip gameBGM;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip mergeSfx;
    [SerializeField] private AudioClip uiClickClip;
    [SerializeField] private AudioClip winSfx;
    [SerializeField] private AudioClip loseSfx;

    private bool isSoundEnabled = true;
    private bool isMusicEnabled = true;

    private Queue<AudioSource> audioSourcePool = new Queue<AudioSource>();
    private List<AudioSource> activeAudioSources = new List<AudioSource>();
    private AudioSource bgmSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSoundManager();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Application.targetFrameRate = 100;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StopAllCoroutines();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "LobbyScene") PlayBGM(lobbyBGM);
        else if (scene.name == "GameScene") PlayBGM(gameBGM);
    }

    private void InitializeSoundManager()
    {
        for (int i = 0; i < initialPoolSize; i++)
            CreateNewAudioSource();

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.volume = 0.5f;
        bgmSource.playOnAwake = false;

        isSoundEnabled = PlayerPrefs.GetInt("IsSoundOn", 1) == 1;
        isMusicEnabled = PlayerPrefs.GetInt("IsMusicOn", 1) == 1;
    }

    private AudioSource CreateNewAudioSource()
    {
        var audioObj = new GameObject("PooledAudioSource");
        audioObj.transform.SetParent(transform);
        var audioSource = audioObj.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = 1f;
        audioSourcePool.Enqueue(audioSource);
        return audioSource;
    }

    private AudioSource GetAudioSource()
    {
        while (audioSourcePool.Count > 0)
        {
            var source = audioSourcePool.Dequeue();
            if (source != null && !source.isPlaying)
            {
                activeAudioSources.Add(source);
                return source;
            }
        }

        if (activeAudioSources.Count < maxPoolSize)
        {
            var source = CreateNewAudioSource();
            audioSourcePool.Dequeue();
            activeAudioSources.Add(source);
            return source;
        }

        var oldest = activeAudioSources[0];
        oldest.Stop();
        return oldest;
    }

    private void ReturnAudioSource(AudioSource audioSource)
    {
        if (audioSource == null) return;
        audioSource.clip = null;
        audioSource.Stop();
        activeAudioSources.Remove(audioSource);
        if (!audioSourcePool.Contains(audioSource))
            audioSourcePool.Enqueue(audioSource);
    }

    public void PlayMergeSFX() => PlaySFX(mergeSfx);
    public void PlayUIClickSFX() => PlaySFX(uiClickClip);
    public void PlayWinSFX() => PlaySFX(winSfx);
    public void PlayLoseSFX() => PlaySFX(loseSfx);
    public void PlayLobbyBGM() => PlayBGM(lobbyBGM);

    public void PlaySFX(AudioClip clip)
    {
        if (!isSoundEnabled || clip == null) return;

        var audioSource = GetAudioSource();
        audioSource.clip = clip;
        audioSource.volume = 1f;
        audioSource.Play();

        StartCoroutine(ReturnToPoolAfterPlay(audioSource, clip.length));
    }

    private IEnumerator ReturnToPoolAfterPlay(AudioSource audioSource, float delay)
    {
        yield return new WaitForSeconds(delay + 0.1f);
        ReturnAudioSource(audioSource);
    }

    private void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.loop = true;

        if (isMusicEnabled) bgmSource.Play();
    }

    public void SetSoundEnabled(bool enabled)
    {
        isSoundEnabled = enabled;
        PlayerPrefs.SetInt("IsSoundOn", enabled ? 1 : 0);
        PlayerPrefs.Save();
        if (!enabled) StopAllSFX();
    }

    public void SetMusicEnabled(bool enabled)
    {
        isMusicEnabled = enabled;
        PlayerPrefs.SetInt("IsMusicOn", enabled ? 1 : 0);
        PlayerPrefs.Save();

        if (bgmSource == null) return;
        if (enabled) { if (!bgmSource.isPlaying && bgmSource.clip != null) bgmSource.Play(); }
        else bgmSource.Pause();
    }

    public bool IsSoundEnabled() => isSoundEnabled;
    public bool IsMusicEnabled() => isMusicEnabled;

    public void StopAllSFX()
    {
        foreach (var source in activeAudioSources)
            if (source != null && source.isPlaying) source.Stop();

        while (activeAudioSources.Count > 0)
            ReturnAudioSource(activeAudioSources[0]);
    }

    public void FadeBGM(float targetVolume, float duration)
    {
        StartCoroutine(FadeBGMCoroutine(targetVolume, duration));
    }

    private IEnumerator FadeBGMCoroutine(float targetVolume, float duration)
    {
        float startVolume = bgmSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        bgmSource.volume = targetVolume;
    }
}