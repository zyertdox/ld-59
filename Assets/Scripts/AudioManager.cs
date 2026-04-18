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
}
