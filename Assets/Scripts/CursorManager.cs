using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    enum State { None, Idle, Click, Passive, Active }

    Texture2D idleTexture;
    Texture2D clickTexture;
    Texture2D passiveTexture;
    Texture2D activeTexture;
    Vector2 hotspot = new Vector2(16, 10);
    State current = State.None;

    public static CursorManager GetOrCreate()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("CursorManager");
        return go.AddComponent<CursorManager>();
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

        idleTexture = LoadCursor("UI/cursor_idle");
        clickTexture = LoadCursor("UI/cursor_click");
        passiveTexture = LoadCursor("UI/cursor_passive");
        activeTexture = LoadCursor("UI/cursor_active");

        SetIdle();
        AttachToSelectables();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachToSelectables();
        SetIdle();
    }

    static void AttachToSelectables()
    {
        var selectables = FindObjectsByType<Selectable>(FindObjectsInactive.Include);
        foreach (var s in selectables)
        {
            if (s.GetComponent<ButtonCursorHover>() == null)
            {
                s.gameObject.AddComponent<ButtonCursorHover>();
            }
        }
    }

    public void SetIdle() => Apply(State.Idle, idleTexture);
    public void SetClick() => Apply(State.Click, clickTexture);
    public void SetPassive() => Apply(State.Passive, passiveTexture);
    public void SetActive() => Apply(State.Active, activeTexture);

    void Apply(State state, Texture2D tex)
    {
        if (current == state) return;
        if (tex == null)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        else
        {
            Cursor.SetCursor(tex, hotspot, CursorMode.Auto);
        }
        current = state;
    }

    static Texture2D LoadCursor(string resourcePath)
    {
        var src = Resources.Load<Texture2D>(resourcePath);
        if (src == null) return null;
        return CopyToCursorTexture(src);
    }

    static Texture2D CopyToCursorTexture(Texture2D src)
    {
        var w = src.width;
        var h = src.height;

        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        var prevActive = RenderTexture.active;
        Graphics.Blit(src, rt);
        RenderTexture.active = rt;

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
#if UNITY_EDITOR
        tex.alphaIsTransparency = true;
#endif
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
        tex.Apply(false, false);

        RenderTexture.active = prevActive;
        RenderTexture.ReleaseTemporary(rt);
        return tex;
    }
}
