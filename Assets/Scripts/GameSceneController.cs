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

    [Header("Brain")] [SerializeField] private RectTransform brainContainer;

    [SerializeField] private GameObject neuronPrefab;
    [SerializeField] private float neuronSize = 100f;
    [SerializeField] private float neuronSpacing = 140f;
    [SerializeField] private float neuronRowY = 380f;

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
            BuildBrainBoard();
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

    private void BuildBrainBoard()
    {
        if (brainContainer == null || neuronPrefab == null) return;
        if (level?.Columns == null || level.Columns.Length == 0) return;

        var inputs = level.Columns[0];
        var outputs = level.Columns[level.Columns.Length - 1];

        SpawnNeuronRow(inputs, -neuronRowY);
        SpawnNeuronRow(outputs, neuronRowY);
    }

    private void SpawnNeuronRow(NeuronNode[] row, float y)
    {
        if (row == null) return;

        var startX = -(row.Length - 1) * neuronSpacing * 0.5f;

        for (var i = 0; i < row.Length; i++)
        {
            var node = row[i];
            var go = Instantiate(neuronPrefab, brainContainer);
            go.name = $"Neuron_{node.Id}";

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(startX + i * neuronSpacing, y);
            rt.sizeDelta = new Vector2(neuronSize, neuronSize);

            var img = go.GetComponent<Image>();
            var label = go.GetComponentInChildren<TMP_Text>();
            ConfigureNeuron(node, img, label);
        }
    }

    private static void ConfigureNeuron(NeuronNode node, Image img, TMP_Text label)
    {
        switch (node)
        {
            case InputNode input:
                if (img != null) img.color = ColorOf(input.TriggerColor);
                if (label != null) label.text = InputLabel(input.TriggerColor);
                break;
            case OutputNode output:
                if (img != null) img.color = new Color(0.75f, 0.75f, 0.8f);
                if (label != null) label.text = OutputArrow(output.Code);
                break;
        }
    }

    private static string InputLabel(TileColor c) => c switch
    {
        TileColor.Red => "R",
        TileColor.Green => "G",
        TileColor.Blue => "B",
        _ => "?"
    };

    private static string OutputArrow(char code) => code switch
    {
        'F' => "↑",
        'U' => "↗",
        'D' => "↖",
        _ => "?"
    };

    private void OnBackClicked()
    {
        SceneManager.LoadScene(levelSelectSceneName);
    }
}