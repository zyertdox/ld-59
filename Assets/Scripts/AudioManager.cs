using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    const string MusicVolumeKey = "audio.music.volume";
    const string SfxVolumeKey = "audio.sfx.volume";
    const float DefaultMusicVolume = 0.75f;
    const float DefaultSfxVolume = 1f;

    AudioSource musicSource;
    AudioSource sfxSource;
    AudioSource loopSource;

    AudioClip clickClip;
    AudioClip moveClip;
    AudioClip[] connectClips;
    AudioClip[] beepClips;
    Coroutine jingleRoutine;

    public float MusicVolume
    {
        get => musicSource.volume;
        set
        {
            float clamped = Mathf.Clamp01(value);
            musicSource.volume = clamped;
            PlayerPrefs.SetFloat(MusicVolumeKey, clamped);
        }
    }

    public float SfxVolume
    {
        get => sfxSource.volume;
        set
        {
            float clamped = Mathf.Clamp01(value);
            sfxSource.volume = clamped;
            if (loopSource != null) loopSource.volume = clamped;
            PlayerPrefs.SetFloat(SfxVolumeKey, clamped);
        }
    }

    public static AudioManager GetOrCreate()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("AudioManager");
        return go.AddComponent<AudioManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume);

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = PlayerPrefs.GetFloat(SfxVolumeKey, DefaultSfxVolume);

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.loop = true;
        loopSource.playOnAwake = false;
        loopSource.volume = sfxSource.volume;

        clickClip = Resources.Load<AudioClip>("Sounds/click");
        moveClip = Resources.Load<AudioClip>("Sounds/move");
        connectClips = new AudioClip[8];
        for (var i = 0; i < connectClips.Length; i++)
        {
            connectClips[i] = Resources.Load<AudioClip>("Sounds/connect_" + (i + 1));
        }

        beepClips = new AudioClip[6];
        for (var i = 0; i < beepClips.Length; i++)
        {
            beepClips[i] = Resources.Load<AudioClip>("Sounds/beep" + (i + 1));
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void StopMusic() => musicSource.Stop();

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void PlayClick() => PlaySfx(clickClip);

    public void StartMoveLoop()
    {
        if (moveClip == null || loopSource == null) return;
        if (loopSource.isPlaying && loopSource.clip == moveClip) return;
        loopSource.clip = moveClip;
        loopSource.volume = sfxSource.volume;
        loopSource.Play();
    }

    public void StopMoveLoop()
    {
        if (loopSource != null) loopSource.Stop();
    }

    public void PlayConnect()
    {
        if (connectClips == null || connectClips.Length == 0) return;
        PlaySfx(connectClips[Random.Range(0, connectClips.Length)]);
    }

    public void PlayWinJingle() => PlayJingle(ascending: true);

    public void PlayLoseJingle() => PlayJingle(ascending: false);

    void PlayJingle(bool ascending)
    {
        if (beepClips == null || beepClips.Length < 2) return;
        if (jingleRoutine != null) StopCoroutine(jingleRoutine);
        jingleRoutine = StartCoroutine(JingleRoutine(ascending));
    }

    IEnumerator JingleRoutine(bool ascending)
    {
        var half = beepClips.Length / 2;
        var highIdx = Random.Range(0, half);
        var lowIdx = Random.Range(half, beepClips.Length);

        var first = ascending ? beepClips[lowIdx] : beepClips[highIdx];
        var second = ascending ? beepClips[highIdx] : beepClips[lowIdx];

        if (first != null)
        {
            sfxSource.PlayOneShot(first);
            yield return new WaitForSeconds(first.length);
        }
        if (second != null)
        {
            sfxSource.PlayOneShot(second);
        }
        jingleRoutine = null;
    }
}
