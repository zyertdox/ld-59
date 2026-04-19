using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] string gameSceneName = "Game";
    [SerializeField] Button playButton;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider sfxSlider;

    void Start()
    {
        var audio = AudioManager.GetOrCreate();
        CursorManager.GetOrCreate();

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(audio.MusicVolume);
            musicSlider.onValueChanged.AddListener(v => audio.MusicVolume = v);
        }
        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(audio.SfxVolume);
            sfxSlider.onValueChanged.AddListener(v => audio.SfxVolume = v);
        }
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
        }
    }

    void OnPlayClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
        SceneManager.LoadScene(gameSceneName);
    }
}
