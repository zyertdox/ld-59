using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TutorialController : MonoBehaviour
{
    const float WaitSeconds = 3f;
    const float FadeDuration = 0.35f;
    const float HoldSeconds = 0.25f;
    const float MoveDuration = 1.0f;
    const float HelperSize = 96f;

    GameSceneController game;
    RectTransform helperRt;
    Image helperImage;
    Sprite helperPassive;
    Sprite helperActive;
    Coroutine routine;
    bool finished;

    public static void TryAttach(GameSceneController gameController)
    {
        if (gameController == null) return;
        if (GameSession.CurrentLevelId != "01") return;
        if (GameSession.UnlockedLevels > 1) return;
        if (gameController.gameObject.GetComponent<TutorialController>() != null) return;

        var tutorial = gameController.gameObject.AddComponent<TutorialController>();
        tutorial.Initialize(gameController);
    }

    public void Initialize(GameSceneController gameController)
    {
        game = gameController;

        helperPassive = Resources.Load<Sprite>("UI/helper_passive");
        helperActive = Resources.Load<Sprite>("UI/helper_active");
        if (helperPassive == null || helperActive == null) return;

        CreateHelper();

        game.OnDragStarted += HandleGrabbed;
        game.OnDragFinished += HandleDragFinished;

        StartCycle();
    }

    void OnDestroy()
    {
        if (game != null)
        {
            game.OnDragStarted -= HandleGrabbed;
            game.OnDragFinished -= HandleDragFinished;
        }
    }

    void CreateHelper()
    {
        var parent = game.NeuronParent;
        if (parent == null) return;

        var go = new GameObject("TutorialHelper");
        go.transform.SetParent(parent, false);

        helperRt = go.AddComponent<RectTransform>();
        helperRt.anchorMin = helperRt.anchorMax = new Vector2(0.5f, 0.5f);
        // hotspot курсора (16, 10) в 64×64 → отражённый по X helper: (48, 10)
        // pivot (unity UI, Y от низа): (48/64, 1 - 10/64) = (0.75, 0.844)
        helperRt.pivot = new Vector2(0.75f, 0.844f);
        helperRt.sizeDelta = new Vector2(HelperSize, HelperSize);

        go.AddComponent<CanvasRenderer>();
        helperImage = go.AddComponent<Image>();
        helperImage.sprite = helperPassive;
        helperImage.raycastTarget = false;
        SetAlpha(0f);

        helperRt.SetAsLastSibling();
    }

    Vector2 GetInputPos()
    {
        var input = game.Level?.Columns?[0]?.FirstOrDefault();
        if (input == null) return Vector2.zero;
        return game.NeuronViews.TryGetValue(input.Id, out var rt) ? rt.anchoredPosition : Vector2.zero;
    }

    Vector2 GetOutputPos()
    {
        var cols = game.Level?.Columns;
        if (cols == null || cols.Length < 2) return Vector2.zero;
        var output = cols[cols.Length - 1]?.FirstOrDefault();
        if (output == null) return Vector2.zero;
        return game.NeuronViews.TryGetValue(output.Id, out var rt) ? rt.anchoredPosition : Vector2.zero;
    }

    void StartCycle()
    {
        if (finished || helperRt == null) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(CycleRoutine());
    }

    void StopCycle()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        if (helperImage != null) SetAlpha(0f);
    }

    IEnumerator CycleRoutine()
    {
        while (!finished)
        {
            yield return new WaitForSeconds(WaitSeconds);

            helperImage.sprite = helperPassive;
            helperRt.anchoredPosition = GetInputPos();
            yield return Fade(0f, 1f, FadeDuration);

            helperImage.sprite = helperActive;
            yield return new WaitForSeconds(HoldSeconds);

            yield return Move(GetInputPos(), GetOutputPos(), MoveDuration);

            helperImage.sprite = helperPassive;
            yield return new WaitForSeconds(HoldSeconds);

            yield return Fade(1f, 0f, FadeDuration);
        }
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        var t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(t / duration)));
            yield return null;
        }
        SetAlpha(to);
    }

    IEnumerator Move(Vector2 from, Vector2 to, float duration)
    {
        var t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            var k = Mathf.Clamp01(t / duration);
            helperRt.anchoredPosition = Vector2.Lerp(from, to, k);
            yield return null;
        }
        helperRt.anchoredPosition = to;
    }

    void SetAlpha(float a)
    {
        if (helperImage == null) return;
        var c = helperImage.color;
        c.a = a;
        helperImage.color = c;
    }

    void HandleGrabbed()
    {
        StopCycle();
    }

    void HandleDragFinished(bool success)
    {
        if (success)
        {
            finished = true;
            if (helperRt != null) Destroy(helperRt.gameObject);
        }
        else
        {
            StartCycle();
        }
    }
}
