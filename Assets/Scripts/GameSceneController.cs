using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
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
    [SerializeField] private Sprite[] crackSprites;
    [SerializeField, Range(0f, 1f)] private float crackProbability = 0.6f;
    [SerializeField, Range(0f, 1f)] private float crackAlpha = 0.6f;

    [Header("Brain")] [SerializeField] private RectTransform brainContainer;

    [SerializeField] private RectTransform neuronField;
    [SerializeField] private GameObject neuronPrefab;
    [SerializeField] private GameObject wirePrefab;
    [SerializeField] private float neuronSize = 100f;
    [SerializeField] private float neuronSpacing = 140f;
    [SerializeField] private float neuronRowY = 380f;
    [SerializeField] private float neuronRowPadding = 40f;
    [SerializeField] private float traceStagger = 70f;
    [SerializeField] private Sprite arrowSprite;
    [SerializeField] private Sprite ledSprite;
    [SerializeField] private Sprite jumperSprite;
    [SerializeField] private float jumperLength = 36f;
    [SerializeField] private float jumperThickness = 18f;

    private RectTransform NeuronParent => neuronField != null ? neuronField : brainContainer;

    [Header("Playback")] [SerializeField] private float stepDuration = 0.35f;

    [SerializeField] private float pauseBetweenSteps = 0.05f;
    [SerializeField] private Toggle fastToggle;
    [SerializeField] private float fastMultiplier = 3f;

    [Header("Popup")] [SerializeField] private GameObject winPopup;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button toListButton;
    [SerializeField] private TMP_Text winLabel;
    [SerializeField] private TMP_Text statusLabel;

    private string[] levelOrder = Array.Empty<string>();

    private readonly Dictionary<string, RectTransform> neuronViews = new();
    private readonly Dictionary<string, GameObject> wireVisuals = new();
    private readonly List<GameObject> jumperVisuals = new();
    private readonly Dictionary<string, Color> baseColors = new();
    private readonly HashSet<string> currentlyHighlighted = new();

    private BrainData brain;
    private NeuronView dragSource;

    private LevelData level;
    private Coroutine playbackRoutine;
    private GameObject tempWireGo;
    private GameObject unitInstance;
    private UnitView unitView;

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

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextClicked);
        }

        if (toListButton != null)
        {
            toListButton.onClick.AddListener(OnBackClicked);
        }

        LoadLevelOrder();
        HidePopup();

        LoadLevel();

        if (level != null)
        {
            BuildGrid();
            SpawnUnit();
            BuildBrainBoard();

            brain = new BrainData();
            DrawWires(brain);

            if (unitInstance != null) unitInstance.transform.SetAsLastSibling();
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
                rt.sizeDelta = new Vector2(tileSize, tileSize);

                var img = tile.GetComponent<Image>();
                if (img != null)
                {
                    img.color = ColorOf(color);
                }

                TryApplyCrack(tile, lx, ly);
            }
        }
    }

    private void TryApplyCrack(GameObject tile, int lx, int ly)
    {
        if (crackSprites == null || crackSprites.Length == 0) return;

        var crackTransform = tile.transform.Find("Crack");
        if (crackTransform == null) return;

        var rng = new System.Random(lx * 73856093 ^ ly * 19349663);
        if (rng.NextDouble() >= crackProbability) return;

        var sprite = crackSprites[rng.Next(crackSprites.Length)];
        var crackImg = crackTransform.GetComponent<Image>();
        if (crackImg == null) return;

        crackImg.sprite = sprite;
        var c = Palette.TileCrack;
        c.a = crackAlpha;
        crackImg.color = c;

        var rotation = rng.Next(0, 4) * 90f;
        var flipX = rng.Next(0, 2) == 0 ? 1f : -1f;
        var flipY = rng.Next(0, 2) == 0 ? 1f : -1f;

        var rt = crackTransform as RectTransform;
        if (rt != null)
        {
            rt.localRotation = Quaternion.Euler(0f, 0f, rotation);
            rt.localScale = new Vector3(flipX, flipY, 1f);
        }

        crackTransform.gameObject.SetActive(true);
    }

    private void SpawnUnit()
    {
        if (fieldContainer == null || unitPrefab == null)
        {
            return;
        }

        unitInstance = Instantiate(unitPrefab, fieldContainer);
        unitInstance.name = "Unit";
        unitView = unitInstance.GetComponent<UnitView>();

        var rt = unitInstance.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = LogicalToVisual(level.Start);
        rt.sizeDelta = new Vector2(tileSize * 0.9f, tileSize * 0.9f * 1.2f);

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
            TileColor.Red => Palette.SignalRed,
            TileColor.Yellow => Palette.SignalYellow,
            TileColor.Blue => Palette.SignalBlue,
            TileColor.Wall => Palette.Wall,
            _ => Color.clear
        };
    }

    private void BuildBrainBoard()
    {
        if (NeuronParent == null || neuronPrefab == null)
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

        var rowY = CalculateRowY();

        SpawnNeuronRow(inputs, -rowY);
        SpawnNeuronRow(outputs, rowY);
    }

    private float CalculateRowY()
    {
        if (neuronField == null) return neuronRowY;
        var fieldHeight = neuronField.rect.height;
        return fieldHeight * 0.5f - neuronSize * 0.5f - neuronRowPadding;
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
            var go = Instantiate(neuronPrefab, NeuronParent);
            go.name = $"Neuron_{node.Id}";

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(startX + i * neuronSpacing, y);
            rt.sizeDelta = new Vector2(neuronSize, neuronSize);

            var img = go.GetComponent<Image>();
            var label = go.GetComponentInChildren<TMP_Text>();
            var iconTransform = go.transform.Find("Icon");
            var iconImg = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            ConfigureNeuron(node, img, iconImg, label);

            var view = go.GetComponent<NeuronView>();
            if (view == null)
            {
                view = go.AddComponent<NeuronView>();
            }

            view.Node = node;
            view.Controller = this;

            neuronViews[node.Id] = rt;
            if (img != null) baseColors[node.Id] = img.color;
        }
    }

    private void DrawWires(BrainData brain)
    {
        if (NeuronParent == null || wirePrefab == null || brain == null)
        {
            return;
        }

        wireVisuals.Clear();

        var total = brain.Wires.Count;
        for (var i = 0; i < brain.Wires.Count; i++)
        {
            var wire = brain.Wires[i];
            if (!neuronViews.TryGetValue(wire.From.Id, out var fromRt)) continue;
            if (!neuronViews.TryGetValue(wire.To.Id, out var toRt)) continue;

            var go = DrawWire(wire.Id, fromRt.anchoredPosition, toRt.anchoredPosition, i, total);
            wireVisuals[wire.Id] = go;
        }

        DrawJumpers(brain);
    }

    private void DrawJumpers(BrainData brain)
    {
        foreach (var j in jumperVisuals)
        {
            if (j != null) Destroy(j);
        }
        jumperVisuals.Clear();

        if (jumperSprite == null || brain?.Wires == null || brain.Wires.Count < 2) return;

        var paths = new List<(List<Vector2Int> path, float fromY, float toY)>();
        foreach (var wire in brain.Wires)
        {
            if (!neuronViews.TryGetValue(wire.From.Id, out var fromRt) ||
                !neuronViews.TryGetValue(wire.To.Id, out var toRt))
            {
                paths.Add((null, 0, 0));
                continue;
            }

            var fromCol = GridColFromX(fromRt.anchoredPosition.x);
            var toCol = GridColFromX(toRt.anchoredPosition.x);
            paths.Add((GetWirePath(fromCol, toCol), fromRt.anchoredPosition.y, toRt.anchoredPosition.y));
        }

        for (var i = 0; i < paths.Count; i++)
        {
            var a = paths[i];
            if (a.path == null) continue;
            for (var j = i + 1; j < paths.Count; j++)
            {
                var b = paths[j];
                if (b.path == null) continue;
                PlaceJumpersBetween(a.path, b.path, a.fromY, a.toY);
            }
        }
    }

    private void PlaceJumpersBetween(List<Vector2Int> pathA, List<Vector2Int> pathB, float fromY, float toY)
    {
        for (var i = 0; i < pathA.Count - 1; i++)
        {
            var a1 = pathA[i];
            var a2 = pathA[i + 1];
            for (var j = 0; j < pathB.Count - 1; j++)
            {
                var b1 = pathB[j];
                var b2 = pathB[j + 1];

                if (!TryGetCrossing(a1, a2, b1, b2, out var cross, out var overheadHorizontal)) continue;

                var pixel = new Vector2(GridXToPixel(cross.x), GridYToPixel(cross.y, fromY, toY));
                SpawnJumper(pixel, overheadHorizontal);
            }
        }
    }

    private static bool TryGetCrossing(Vector2Int a1, Vector2Int a2, Vector2Int b1, Vector2Int b2, out Vector2Int cross, out bool overheadHorizontal)
    {
        cross = default;
        overheadHorizontal = false;

        var aHoriz = a1.y == a2.y;
        var bHoriz = b1.y == b2.y;
        if (aHoriz == bHoriz) return false;

        var hSegA = aHoriz ? a1 : b1;
        var hSegB = aHoriz ? a2 : b2;
        var vSegA = aHoriz ? b1 : a1;
        var vSegB = aHoriz ? b2 : a2;

        var hY = hSegA.y;
        var hXmin = Mathf.Min(hSegA.x, hSegB.x);
        var hXmax = Mathf.Max(hSegA.x, hSegB.x);
        var vX = vSegA.x;
        var vYmin = Mathf.Min(vSegA.y, vSegB.y);
        var vYmax = Mathf.Max(vSegA.y, vSegB.y);

        if (vX <= hXmin || vX >= hXmax) return false;
        if (hY <= vYmin || hY >= vYmax) return false;

        cross = new Vector2Int(vX, hY);
        overheadHorizontal = !aHoriz;
        return true;
    }

    private void SpawnJumper(Vector2 pixelPoint, bool overheadHorizontal)
    {
        var go = new GameObject("Jumper");
        go.AddComponent<CanvasRenderer>();
        var rt = go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();

        img.sprite = jumperSprite;
        img.color = Color.white;
        img.raycastTarget = false;

        rt.SetParent(NeuronParent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pixelPoint;
        rt.sizeDelta = new Vector2(jumperLength, jumperThickness);
        rt.localEulerAngles = Vector3.zero;
        rt.SetAsLastSibling();

        jumperVisuals.Add(go);
    }

    private void RedrawAllWires()
    {
        foreach (var go in wireVisuals.Values)
        {
            if (go != null) Destroy(go);
        }
        wireVisuals.Clear();
        DrawWires(brain);
    }

    private GameObject DrawWire(string id, Vector2 fromPos, Vector2 toPos, int index, int total)
    {
        var trace = new GameObject($"Wire_{id}");
        var rt = trace.AddComponent<RectTransform>();
        rt.SetParent(NeuronParent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        trace.transform.SetAsFirstSibling();

        var fromCol = GridColFromX(fromPos.x);
        var toCol = GridColFromX(toPos.x);
        var path = GetWirePath(fromCol, toCol);

        if (path == null)
        {
            SpawnTraceSegment(trace.transform, fromPos, toPos);
        }
        else
        {
            var pixels = new List<Vector2>(path.Count);
            foreach (var gp in path)
            {
                pixels.Add(new Vector2(GridXToPixel(gp.x), GridYToPixel(gp.y, fromPos.y, toPos.y)));
            }

            for (var i = 0; i < pixels.Count - 1; i++)
            {
                SpawnTraceSegment(trace.transform, pixels[i], pixels[i + 1]);
            }
        }

        baseColors[id] = Palette.PcbCopper;
        return trace;
    }

    private int GridColFromX(float x)
    {
        return Mathf.RoundToInt(x / neuronSpacing * 3f + 3f);
    }

    private float GridXToPixel(int col)
    {
        return neuronSpacing * (col - 3) / 3f;
    }

    private static float GridYToPixel(int row, float fromY, float toY)
    {
        return fromY + (toY - fromY) * row / 6f;
    }

    private static List<Vector2Int> GetWirePath(int fromCol, int toCol)
    {
        return (fromCol, toCol) switch
        {
            (0, 0) => new List<Vector2Int> { new(0, 0), new(0, 6) },
            (0, 3) => new List<Vector2Int> { new(0, 0), new(0, 3), new(2, 3), new(2, 5), new(3, 5), new(3, 6) },
            (0, 6) => new List<Vector2Int> { new(0, 0), new(0, 1), new(5, 1), new(5, 5), new(5, 6), new(6, 6) },
            (3, 0) => new List<Vector2Int> { new(3, 0), new(3, 4), new(0, 4), new(0, 6) },
            (3, 3) => new List<Vector2Int> { new(3, 0), new(3, 6) },
            (3, 6) => new List<Vector2Int> { new(3, 0), new(3, 4), new(6, 4), new(6, 6) },
            (6, 0) => new List<Vector2Int> { new(6, 0), new(6, 2), new(1, 2), new(1, 5), new(0, 5), new(0, 6) },
            (6, 3) => new List<Vector2Int> { new(6, 0), new(6, 3), new(4, 3), new(4, 5), new(3, 5), new(3, 6) },
            (6, 6) => new List<Vector2Int> { new(6, 0), new(6, 6) },
            _ => null
        };
    }

    private void SpawnTraceSegment(Transform parent, Vector2 a, Vector2 b)
    {
        var delta = b - a;
        var length = delta.magnitude;
        if (length < 0.5f) return;

        var go = Instantiate(wirePrefab, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = a;

        var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        rt.sizeDelta = new Vector2(length, rt.sizeDelta.y);
        rt.localEulerAngles = new Vector3(0f, 0f, angle);

        var img = go.GetComponent<Image>();
        if (img != null) img.color = Palette.PcbCopper;
    }

    private void ConfigureNeuron(NeuronNode node, Image chipImg, Image iconImg, TMP_Text label)
    {
        if (chipImg != null) chipImg.color = Color.white;
        if (label != null) label.text = string.Empty;

        if (iconImg == null) return;

        switch (node)
        {
            case InputNode input:
                iconImg.sprite = ledSprite;
                iconImg.color = ColorOf(input.TriggerColor);
                iconImg.transform.localRotation = Quaternion.identity;
                break;
            case OutputNode output:
                iconImg.sprite = arrowSprite;
                iconImg.color = Palette.PcbGold;
                var rot = output.Code switch
                {
                    'F' => 0f,
                    'U' => -45f,
                    'D' => 45f,
                    _ => 0f
                };
                iconImg.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
                break;
        }
    }

    private static string InputLabel(TileColor c)
    {
        return c switch
        {
            TileColor.Red => "R",
            TileColor.Yellow => "Y",
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

    private void ApplyHighlights(IList<string> ids)
    {
        ClearHighlights();
        if (ids == null) return;

        foreach (var id in ids)
        {
            SetHighlighted(id, true);
            currentlyHighlighted.Add(id);
        }
    }

    private void ClearHighlights()
    {
        foreach (var id in currentlyHighlighted)
        {
            SetHighlighted(id, false);
        }

        currentlyHighlighted.Clear();
    }

    private void SetHighlighted(string id, bool active)
    {
        Image img = null;

        if (neuronViews.TryGetValue(id, out var neuronRt))
        {
            img = neuronRt.GetComponent<Image>();
        }
        else if (wireVisuals.TryGetValue(id, out var wireGo))
        {
            var images = wireGo.GetComponentsInChildren<Image>(true);
            foreach (var traceImg in images)
            {
                if (active)
                {
                    traceImg.color = Palette.Highlight;
                }
                else if (baseColors.TryGetValue(id, out var baseColor))
                {
                    traceImg.color = baseColor;
                }
            }
            return;
        }

        if (img == null) return;

        if (active)
        {
            img.color = Palette.Highlight;
        }
        else if (baseColors.TryGetValue(id, out var baseColor))
        {
            img.color = baseColor;
        }
    }

    private IEnumerator PlaySimulation(List<MoveCommand> commands)
    {
        if (unitInstance == null)
        {
            yield break;
        }

        var rt = unitInstance.GetComponent<RectTransform>();

        IList<string> lastRunningHighlights = null;

        try
        {
            foreach (var cmd in commands)
            {
                if (cmd.Status == UnitStatus.Running)
                {
                    ApplyHighlights(cmd.Highlights);
                    lastRunningHighlights = cmd.Highlights;

                    var dy = cmd.To.y - cmd.From.y;
                    if (unitView != null)
                    {
                        if (dy > 0) unitView.SetState(UnitView.State.MoveRight);
                        else if (dy < 0) unitView.SetState(UnitView.State.MoveLeft);
                        else unitView.SetState(UnitView.State.MoveForward);
                    }

                    if (AudioManager.Instance != null) AudioManager.Instance.StartMoveLoop();

                    var fromPos = LogicalToVisual(cmd.From);
                    var toPos = LogicalToVisual(cmd.To);

                    var elapsed = 0f;
                    var duration = CurrentStepDuration;
                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        duration = CurrentStepDuration;
                        rt.anchoredPosition = Vector2.Lerp(fromPos, toPos, Mathf.Clamp01(elapsed / duration));
                        yield return null;
                    }

                    rt.anchoredPosition = toPos;
                    ClearHighlights();

                    if (CurrentPause > 0f)
                    {
                        yield return new WaitForSeconds(CurrentPause);
                    }
                }
                else
                {
                    Debug.Log($"Simulation ended: {cmd.Status}");

                    if (cmd.Status == UnitStatus.Crashed && lastRunningHighlights != null)
                    {
                        ApplyHighlights(lastRunningHighlights);
                    }
                    else if (cmd.Status == UnitStatus.Stuck)
                    {
                        ApplyHighlights(cmd.Highlights);
                    }

                    if (unitView != null)
                    {
                        if (cmd.Status == UnitStatus.Won) unitView.SetState(UnitView.State.Success);
                        else unitView.SetState(UnitView.State.Crashed);
                    }

                    playbackRoutine = null;
                    OnSimulationEnded(cmd.Status);
                    yield break;
                }
            }

            ClearHighlights();
            playbackRoutine = null;
        }
        finally
        {
            if (AudioManager.Instance != null) AudioManager.Instance.StopMoveLoop();
        }
    }

    private void OnBackClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
        SceneManager.LoadScene(levelSelectSceneName);
    }

    private void OnPlayClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();

        if (level == null || brain == null)
        {
            return;
        }

        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }

        ResetUnit();
        ClearStatusLabel();
        ClearHighlights();
        HidePopup();
        unitView?.SetState(UnitView.State.Thinking);

        var commands = Simulator.Simulate(level, brain);
        playbackRoutine = StartCoroutine(PlaySimulation(commands));
    }

    private void OnResetClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();

        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }

        ResetUnit();
        ClearStatusLabel();
        ClearHighlights();
        HidePopup();
        unitView?.SetState(UnitView.State.Thinking);
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

    private void OnSimulationEnded(UnitStatus status)
    {
        switch (status)
        {
            case UnitStatus.Won:
                if (AudioManager.Instance != null) AudioManager.Instance.PlayWinJingle();
                ShowWin();
                break;
            case UnitStatus.Crashed:
                if (AudioManager.Instance != null) AudioManager.Instance.PlayLoseJingle();
                SetStatusLabel("Crashed");
                break;
            case UnitStatus.Stuck:
                if (AudioManager.Instance != null) AudioManager.Instance.PlayLoseJingle();
                SetStatusLabel("Stuck");
                break;
        }
    }

    private void ShowWin()
    {
        if (winPopup != null) winPopup.SetActive(true);
        if (winLabel != null) winLabel.text = "You Won!";
    }

    private void HidePopup()
    {
        if (winPopup != null) winPopup.SetActive(false);
    }

    private void SetStatusLabel(string text)
    {
        if (statusLabel == null) return;
        statusLabel.gameObject.SetActive(true);
        statusLabel.text = text;
    }

    private void ClearStatusLabel()
    {
        if (statusLabel == null) return;
        statusLabel.text = string.Empty;
        statusLabel.gameObject.SetActive(false);
    }

    private void OnNextClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();

        var current = GameSession.CurrentLevelId;
        var idx = Array.IndexOf(levelOrder, current);
        if (idx < 0 || idx >= levelOrder.Length - 1)
        {
            SceneManager.LoadScene(levelSelectSceneName);
            return;
        }

        GameSession.CurrentLevelId = levelOrder[idx + 1];
        ReloadLevel();
    }

    private void ReloadLevel()
    {
        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }

        if (tempWireGo != null)
        {
            Destroy(tempWireGo);
            tempWireGo = null;
        }

        dragSource = null;
        unitInstance = null;

        ClearContainer(fieldContainer);
        ClearContainer(NeuronParent);
        neuronViews.Clear();
        wireVisuals.Clear();
        jumperVisuals.Clear();
        baseColors.Clear();
        currentlyHighlighted.Clear();

        HidePopup();
        ClearStatusLabel();

        LoadLevel();
        if (level != null)
        {
            BuildGrid();
            SpawnUnit();
            BuildBrainBoard();
            brain = new BrainData();
            DrawWires(brain);

            if (unitInstance != null) unitInstance.transform.SetAsLastSibling();
        }
    }

    private static void ClearContainer(RectTransform container)
    {
        if (container == null) return;
        for (var i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }
    }

    private void LoadLevelOrder()
    {
        var asset = Resources.Load<TextAsset>("Levels/levels");
        if (asset == null)
        {
            levelOrder = Array.Empty<string>();
            return;
        }

        var manifest = JsonConvert.DeserializeObject<LevelManifestDto>(asset.text);
        if (manifest?.levels == null)
        {
            levelOrder = Array.Empty<string>();
            return;
        }

        levelOrder = new string[manifest.levels.Length];
        for (var i = 0; i < manifest.levels.Length; i++)
        {
            levelOrder[i] = manifest.levels[i].id;
        }
    }

    [Serializable]
    private class LevelManifestDto
    {
        public LevelEntryDto[] levels;
    }

    [Serializable]
    private class LevelEntryDto
    {
        public string id;
        public string name;
    }

    private static void OnFastToggleChanged(bool isOn)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
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

        tempWireGo = Instantiate(wirePrefab, NeuronParent);
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
            if (AudioManager.Instance != null) AudioManager.Instance.PlayConnect();
        }

        if (tempWireGo != null)
        {
            Destroy(tempWireGo);
        }

        tempWireGo = null;
        dragSource = null;

        RedrawAllWires();
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
            NeuronParent, eventData.position, eventData.pressEventCamera, out var pointerLocal);

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