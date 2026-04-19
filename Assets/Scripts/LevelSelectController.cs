using System;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectController : MonoBehaviour
{
    [SerializeField] private Transform container;
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_Text levelName;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameSceneName = "Game";

    private void Start()
    {
        AudioManager.GetOrCreate();

        if (levelName != null)
        {
            levelName.text = string.Empty;
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        LoadManifestAndPopulate();
    }

    private void LoadManifestAndPopulate()
    {
        var asset = Resources.Load<TextAsset>("Levels/levels");
        if (asset == null)
        {
            Debug.LogError("Levels manifest not found at Resources/Levels/levels.json");
            return;
        }

        var manifest = JsonConvert.DeserializeObject<Manifest>(asset.text);
        if (manifest?.levels == null)
        {
            return;
        }

        foreach (var entry in manifest.levels)
        {
            SpawnButton(entry);
        }
    }

    private void SpawnButton(LevelEntry entry)
    {
        var instance = Instantiate(buttonPrefab, container);

        var text = instance.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.text = entry.id;
        }

        var button = instance.GetComponentInChildren<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnLevelClicked(entry.id));
        }

        var trigger = instance.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = instance.AddComponent<EventTrigger>();
        }

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => SetLevelName(entry.name));
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => SetLevelName(string.Empty));
        trigger.triggers.Add(exit);
    }

    private void SetLevelName(string value)
    {
        if (levelName != null)
        {
            levelName.text = value;
        }
    }

    private void OnLevelClicked(string id)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
        GameSession.CurrentLevelId = id;
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnBackClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    [Serializable]
    private class Manifest
    {
        public LevelEntry[] levels;
    }

    [Serializable]
    private class LevelEntry
    {
        public string id;
        public string name;
    }
}