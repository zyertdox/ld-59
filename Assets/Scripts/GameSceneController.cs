using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSceneController : MonoBehaviour
{
    [SerializeField] private string levelSelectSceneName = "LevelSelect";
    [SerializeField] private Button backButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private TMP_Text levelLabel;

    [Header("Field")] [SerializeField] private RectTransform fieldContainer;

    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject unitPrefab;
    [SerializeField] private float tileSize = 80f;

    [Header("Brain")] [SerializeField] private RectTransform brainContainer;

    [SerializeField] private GameObject neuronPrefab;
    [SerializeField] private GameObject wirePrefab;
    [SerializeField] private float neuronSize = 100f;
    [SerializeField] private float neuronSpacing = 140f;
    [SerializeField] private float neuronRowY = 380f;

    [Header("Playback")] [SerializeField] private float stepDuration = 0.35f;

    [SerializeField] private float pauseBetweenSteps = 0.05f;
    [SerializeField] private Toggle fastToggle;
    [SerializeField] private float fastMultiplier = 3f;

    private readonly Dictionary<string, RectTransform> neuronViews = new();
    private readonly Dictionary<string, GameObject> wireVisuals = new();
    private BrainData brain;
    private NeuronView dragSource;

    private LevelData level;
    private Coroutine playbackRoutine;
    private GameObject tempWireGo;
    private GameObject unitInstance;

    private float CurrentStepDuration => IsFast ? stepDuration / fastMultiplier : stepDuration;
    private float CurrentPause => IsFast ? pauseBetweenSteps / fastMultiplier : pauseBetweenSteps;
    private bool IsFast => fastToggle != null && fastToggle.isOn;

    private void Start()
    {
        AudioManager.GetOrCreate();

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
        }

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetClicked);
        }

        if (fastToggle != null)
        {
            fastToggle.SetIsOnWithoutNotify(GameSession.FastPlayback);
            fastToggle.onValueChanged.AddListener(OnFastToggleChanged);
        }

        LoadLevel();

        if (level != null)
        {
            BuildGrid();
            SpawnUnit();
            BuildBrainBoard();

            brain = new BrainData();
            DrawWires(brain);
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
        if (brainContainer == null || neuronPrefab == null)
        {
            return;
        }

        if (level?.Columns == null || level.Columns.Length == 0)
        {
            return;
        }

        neuronViews.Clear();

        var inputs = level.Columns[0];
        var outputs = level.Columns[level.Columns.Length - 1];

        SpawnNeuronRow(inputs, -neuronRowY);
        SpawnNeuronRow(outputs, neuronRowY);
    }

    private void SpawnNeuronRow(NeuronNode[] row, float y)
    {
        if (row == null)
        {
            return;
        }

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

            var view = go.GetComponent<NeuronView>();
            if (view == null)
            {
                view = go.AddComponent<NeuronView>();
            }

            view.Node = node;
            view.Controller = this;

            neuronViews[node.Id] = rt;
        }
    }

    private void DrawWires(BrainData brain)
    {
        if (brainContainer == null || wirePrefab == null || brain == null)
        {
            return;
        }

        wireVisuals.Clear();

        foreach (var wire in brain.Wires)
        {
            if (!neuronViews.TryGetValue(wire.From.Id, out var fromRt))
            {
                continue;
            }

            if (!neuronViews.TryGetValue(wire.To.Id, out var toRt))
            {
                continue;
            }

            var go = DrawWire(wire.Id, fromRt.anchoredPosition, toRt.anchoredPosition);
            wireVisuals[wire.Id] = go;
        }
    }

    private GameObject DrawWire(string id, Vector2 fromPos, Vector2 toPos)
    {
        var go = Instantiate(wirePrefab, brainContainer);
        go.name = $"Wire_{id}";
        go.transform.SetAsFirstSibling();

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = fromPos;

        var delta = toPos - fromPos;
        var length = delta.magnitude;
        var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        rt.sizeDelta = new Vector2(length, rt.sizeDelta.y);
        rt.localEulerAngles = new Vector3(0f, 0f, angle);
        return go;
    }

    private static void ConfigureNeuron(NeuronNode node, Image img, TMP_Text label)
    {
        switch (node)
        {
            case InputNode input:
                if (img != null)
                {
                    img.color = ColorOf(input.TriggerColor);
                }

                if (label != null)
                {
                    label.text = InputLabel(input.TriggerColor);
                }

                break;
            case OutputNode output:
                if (img != null)
                {
                    img.color = new Color(0.75f, 0.75f, 0.8f);
                }

                if (label != null)
                {
                    label.text = OutputArrow(output.Code);
                }

                break;
        }
    }

    private static string InputLabel(TileColor c)
    {
        return c switch
        {
            TileColor.Red => "R",
            TileColor.Green => "G",
            TileColor.Blue => "B",
            _ => "?"
        };
    }

    private static string OutputArrow(char code)
    {
        return code switch
        {
            'F' => "↑",
            'U' => "↗",
            'D' => "↖",
            _ => "?"
        };
    }

    private IEnumerator PlaySimulation(List<MoveCommand> commands)
    {
        if (unitInstance == null)
        {
            yield break;
        }

        var rt = unitInstance.GetComponent<RectTransform>();

        foreach (var cmd in commands)
        {
            var fromPos = LogicalToVisual(cmd.From);
            var toPos = LogicalToVisual(cmd.To);

            if (fromPos != toPos)
            {
                var elapsed = 0f;
                var duration = CurrentStepDuration;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    duration = CurrentStepDuration;
                    rt.anchoredPosition = Vector2.Lerp(fromPos, toPos, Mathf.Clamp01(elapsed / duration));
                    yield return null;
                }
            }

            rt.anchoredPosition = toPos;

            if (cmd.Status != UnitStatus.Running)
            {
                Debug.Log($"Simulation ended: {cmd.Status}");
                playbackRoutine = null;
                yield break;
            }

            if (CurrentPause > 0f)
            {
                yield return new WaitForSeconds(CurrentPause);
            }
        }

        playbackRoutine = null;
    }

    private void OnBackClicked()
    {
        SceneManager.LoadScene(levelSelectSceneName);
    }

    private void OnPlayClicked()
    {
        if (level == null || brain == null)
        {
            return;
        }

        if (playbackRoutine != null)
        {
            return;
        }

        ResetUnit();
        var commands = Simulator.Simulate(level, brain);
        playbackRoutine = StartCoroutine(PlaySimulation(commands));
    }

    private void OnResetClicked()
    {
        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }

        ResetUnit();
    }

    private void ResetUnit()
    {
        if (unitInstance == null || level == null)
        {
            return;
        }

        var rt = unitInstance.GetComponent<RectTransform>();
        rt.anchoredPosition = LogicalToVisual(level.Start);
    }

    private static void OnFastToggleChanged(bool isOn)
    {
        GameSession.FastPlayback = isOn;
    }

    public void OnNeuronDragStart(NeuronView source, PointerEventData eventData)
    {
        if (source.Node is not InputNode)
        {
            return;
        }

        if (brain == null)
        {
            return;
        }

        dragSource = source;

        RemoveWireFromInput(source.Node.Id);

        tempWireGo = Instantiate(wirePrefab, brainContainer);
        tempWireGo.name = "TempWire";
        tempWireGo.transform.SetAsFirstSibling();

        var tempImg = tempWireGo.GetComponent<Image>();
        if (tempImg != null)
        {
            var c = tempImg.color;
            c.a = 0.5f;
            tempImg.color = c;
        }

        UpdateTempWire(eventData);
    }

    public void OnNeuronDrag(NeuronView source, PointerEventData eventData)
    {
        if (dragSource != source)
        {
            return;
        }

        UpdateTempWire(eventData);
    }

    public void OnNeuronDragEnd(NeuronView source, PointerEventData eventData)
    {
        if (dragSource != source)
        {
            return;
        }

        var target = FindNeuronUnderPointer(eventData);

        if (target != null && target.Node is OutputNode output && dragSource.Node is InputNode input)
        {
            var wire = new Wire(Guid.NewGuid().ToString(), input, output);
            brain.Wires.Add(wire);

            if (neuronViews.TryGetValue(input.Id, out var fromRt) &&
                neuronViews.TryGetValue(output.Id, out var toRt))
            {
                var go = DrawWire(wire.Id, fromRt.anchoredPosition, toRt.anchoredPosition);
                wireVisuals[wire.Id] = go;
            }
        }

        if (tempWireGo != null)
        {
            Destroy(tempWireGo);
        }

        tempWireGo = null;
        dragSource = null;
    }

    private void RemoveWireFromInput(string inputId)
    {
        for (var i = brain.Wires.Count - 1; i >= 0; i--)
        {
            var w = brain.Wires[i];
            if (w.From.Id != inputId)
            {
                continue;
            }

            if (wireVisuals.TryGetValue(w.Id, out var go))
            {
                Destroy(go);
                wireVisuals.Remove(w.Id);
            }

            brain.Wires.RemoveAt(i);
        }
    }

    private void UpdateTempWire(PointerEventData eventData)
    {
        if (tempWireGo == null || dragSource == null)
        {
            return;
        }

        if (!neuronViews.TryGetValue(dragSource.Node.Id, out var fromRt))
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            brainContainer, eventData.position, eventData.pressEventCamera, out var pointerLocal);

        var rt = tempWireGo.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = fromRt.anchoredPosition;

        var delta = pointerLocal - fromRt.anchoredPosition;
        var length = delta.magnitude;
        var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        rt.sizeDelta = new Vector2(length, rt.sizeDelta.y);
        rt.localEulerAngles = new Vector3(0f, 0f, angle);
    }

    private NeuronView FindNeuronUnderPointer(PointerEventData eventData)
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (var r in results)
        {
            var view = r.gameObject.GetComponentInParent<NeuronView>();
            if (view != null)
            {
                return view;
            }
        }

        return null;
    }
}