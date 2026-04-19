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

    static readonly Color LockedColor = new Color(0.56f, 0.56f, 0.56f, 1f);

    private void Start()
    {
        AudioManager.GetOrCreate();
        CursorManager.GetOrCreate();

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

        var unlocked = GameSession.UnlockedLevels;
        for (var i = 0; i < manifest.levels.Length; i++)
        {
            SpawnButton(manifest.levels[i], i >= unlocked);
        }
    }

    private void SpawnButton(LevelEntry entry, bool locked)
    {
        var instance = Instantiate(buttonPrefab, container);

        var text = instance.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.text = entry.id;
            if (locked)
            {
                text.color = LockedColor;
                text.fontStyle |= FontStyles.Strikethrough;
            }
        }

        var button = instance.GetComponentInChildren<Button>();
        if (button != null)
        {
            if (locked)
            {
                button.interactable = false;
            }
            else
            {
                button.onClick.AddListener(() => OnLevelClicked(entry.id));
                if (button.GetComponent<ButtonCursorHover>() == null)
                {
                    button.gameObject.AddComponent<ButtonCursorHover>();
                }
            }
        }

        var trigger = instance.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = instance.AddComponent<EventTrigger>();
        }

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => SetLevelName(entry.name, locked));
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => SetLevelName(string.Empty, false));
        trigger.triggers.Add(exit);
    }

    private void SetLevelName(string value, bool locked)
    {
        if (levelName == null) return;
        levelName.text = value;
        if (locked)
        {
            levelName.color = LockedColor;
            levelName.fontStyle |= FontStyles.Strikethrough;
        }
        else
        {
            levelName.color = Palette.PcbCopper;
            levelName.fontStyle &= ~FontStyles.Strikethrough;
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