using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSceneController : MonoBehaviour
{
    [SerializeField] private string levelSelectSceneName = "LevelSelect";
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_Text levelLabel;

    private LevelData level;

    private void Start()
    {
        AudioManager.GetOrCreate();

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        LoadLevel();
    }

    private void LoadLevel()
    {
        var id = GameSession.CurrentLevelId;
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("GameSession.CurrentLevelId is empty — no level loaded.");
            if (levelLabel != null) levelLabel.text = "(no level selected)";
            return;
        }

        try
        {
            level = LevelLoader.LoadFromResources($"Levels/{id}");
            Debug.Log($"Loaded level {level.Id}: {level.Name} ({level.Width}x{level.Height})");
            if (levelLabel != null) levelLabel.text = $"{level.Id} — {level.Name}";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load level {id}: {e.Message}");
            if (levelLabel != null) levelLabel.text = $"Error loading {id}";
        }
    }

    private void OnBackClicked()
    {
        SceneManager.LoadScene(levelSelectSceneName);
    }
}
