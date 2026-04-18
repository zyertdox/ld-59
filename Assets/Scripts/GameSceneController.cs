using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSceneController : MonoBehaviour
{
    [SerializeField] private string levelSelectSceneName = "LevelSelect";
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_Text levelLabel;

    [Header("Field")] [SerializeField] private RectTransform fieldContainer;

    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject unitPrefab;
    [SerializeField] private float tileSize = 80f;

    private LevelData level;
    private GameObject unitInstance;

    private void Start()
    {
        AudioManager.GetOrCreate();

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        LoadLevel();

        if (level != null)
        {
            BuildGrid();
            SpawnUnit();
        }
    }

    private void LoadLevel()
    {
        var id = GameSession.CurrentLevelId;
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("GameSession.CurrentLevelId is empty — no level loaded.");
            if (levelLabel != null)
            {
                levelLabel.text = "(no level selected)";
            }

            return;
        }

        try
        {
            level = LevelLoader.LoadFromResources($"Levels/{id}");
            Debug.Log($"Loaded level {level.Id}: {level.Name} ({level.Width}x{level.Height})");
            if (levelLabel != null)
            {
                levelLabel.text = $"{level.Id} — {level.Name}";
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load level {id}: {e.Message}");
            if (levelLabel != null)
            {
                levelLabel.text = $"Error loading {id}";
            }
        }
    }

    private void BuildGrid()
    {
        if (fieldContainer == null || tilePrefab == null)
        {
            return;
        }

        for (var lx = 0; lx < level.Width; lx++)
        {
            for (var ly = 0; ly < level.Height; ly++)
            {
                var color = level.Tiles[lx, ly];
                if (color == TileColor.None)
                {
                    continue;
                }

                var tile = Instantiate(tilePrefab, fieldContainer);
                tile.name = $"Tile_{lx}_{ly}";

                var rt = tile.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = LogicalToVisual(new Vector2Int(lx, ly));
                rt.sizeDelta = new Vector2(tileSize - 4, tileSize - 4);

                var img = tile.GetComponent<Image>();
                if (img != null)
                {
                    img.color = ColorOf(color);
                }
            }
        }
    }

    private void SpawnUnit()
    {
        if (fieldContainer == null || unitPrefab == null)
        {
            return;
        }

        unitInstance = Instantiate(unitPrefab, fieldContainer);
        unitInstance.name = "Unit";

        var rt = unitInstance.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = LogicalToVisual(level.Start);
        rt.sizeDelta = new Vector2(tileSize * 0.7f, tileSize * 0.7f);

        rt.SetAsLastSibling();
    }

    private Vector2 LogicalToVisual(Vector2Int logical)
    {
        var visualWidth = level.Height;
        var visualHeight = level.Width;
        var originX = -(visualWidth - 1) * tileSize * 0.5f;
        var originY = -(visualHeight - 1) * tileSize * 0.5f;

        var vx = logical.y;
        var vy = logical.x;

        return new Vector2(originX + vx * tileSize, originY + vy * tileSize);
    }

    private static Color ColorOf(TileColor c)
    {
        return c switch
        {
            TileColor.Red => new Color(0.9f, 0.3f, 0.3f),
            TileColor.Green => new Color(0.35f, 0.8f, 0.4f),
            TileColor.Blue => new Color(0.3f, 0.55f, 0.95f),
            TileColor.Wall => new Color(0.2f, 0.2f, 0.2f),
            _ => Color.clear
        };
    }

    private void OnBackClicked()
    {
        SceneManager.LoadScene(levelSelectSceneName);
    }
}