using UnityEngine;
using UnityEngine.UI;

public class UnitView : MonoBehaviour
{
    public enum State
    {
        Thinking,
        MoveForward,
        MoveLeft,
        MoveRight,
        Success,
        Crashed
    }

    [Header("Parts")]
    [SerializeField] private RectTransform head;
    [SerializeField] private RectTransform shadow;
    [SerializeField] private RectTransform eyesContainer;
    [SerializeField] private Image body;
    [SerializeField] private Image bodyOutline;
    [SerializeField] private Image shadowImage;
    [SerializeField] private Image eyes;
    [SerializeField] private Image eyesOutline;
    [SerializeField] private Image question1;
    [SerializeField] private Image question2;
    [SerializeField] private Image question3;
    [SerializeField] private Image exclamation;

    [Header("Idle Bob")]
    [SerializeField] private float bobAmplitude = 4f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float shadowScaleAmount = 0.12f;

    [Header("Eye Shift")]
    [SerializeField] private float eyeShiftAmount = 6f;

    [Header("Thinking")]
    [SerializeField] private Color[] thinkingColors;
    [SerializeField] private float thinkingStepDuration = 0.5f;

    [Header("Static Colors")]
    [SerializeField] private Color bodyColor = new(0.54f, 0.87f, 0.93f, 1f);
    [SerializeField] private Color outlineColor = new(0.12f, 0.16f, 0.23f, 1f);
    [SerializeField] private Color shadowColor = new(0f, 0f, 0f, 0.4f);
    [SerializeField] private Color eyesColor = new(0.12f, 0.16f, 0.23f, 1f);
    [SerializeField] private Color exclamationColor = new(0.78f, 0.35f, 0.29f, 1f);

    private State currentState = State.MoveForward;
    private int thinkingOffset;
    private float thinkingTimer;
    private Image[] questionImages;

    private void Awake()
    {
        questionImages = new[] { question1, question2, question3 };

        if (thinkingColors == null || thinkingColors.Length == 0)
        {
            thinkingColors = new[]
            {
                new Color(0.78f, 0.35f, 0.29f, 1f),
                new Color(0.84f, 0.68f, 0.29f, 1f),
                new Color(0.35f, 0.56f, 0.75f, 1f)
            };
        }

        ApplyStaticColors();
        SetState(State.Thinking);
    }

    private void ApplyStaticColors()
    {
        if (body != null) body.color = bodyColor;
        if (bodyOutline != null) bodyOutline.color = outlineColor;
        if (shadowImage != null) shadowImage.color = shadowColor;
        if (eyes != null) eyes.color = eyesColor;
        if (eyesOutline != null) eyesOutline.color = outlineColor;
        if (exclamation != null) exclamation.color = exclamationColor;
    }

    private void Update()
    {
        var wave = Mathf.Sin(Time.time * bobSpeed);

        if (head != null)
        {
            head.localPosition = new Vector3(head.localPosition.x, wave * bobAmplitude, head.localPosition.z);
        }

        if (shadow != null)
        {
            var scale = 1f - wave * shadowScaleAmount;
            shadow.localScale = new Vector3(scale, 1f, 1f);
        }

        if (currentState == State.Thinking)
        {
            thinkingTimer += Time.deltaTime;
            if (thinkingTimer >= thinkingStepDuration)
            {
                thinkingTimer = 0f;
                var len = Mathf.Max(1, thinkingColors.Length);
                thinkingOffset = (thinkingOffset + 1) % len;
                UpdateQuestionColors();
            }
        }
    }

    public void SetState(State state)
    {
        currentState = state;

        if (eyesContainer != null) eyesContainer.localPosition = Vector3.zero;

        SetActive(question1, false);
        SetActive(question2, false);
        SetActive(question3, false);
        SetActive(exclamation, false);

        switch (state)
        {
            case State.Thinking:
                SetActive(question1, true);
                SetActive(question2, true);
                SetActive(question3, true);
                thinkingOffset = 0;
                thinkingTimer = 0f;
                UpdateQuestionColors();
                break;
            case State.MoveForward:
                break;
            case State.MoveLeft:
                if (eyesContainer != null)
                    eyesContainer.localPosition = new Vector3(-eyeShiftAmount, 0f, 0f);
                break;
            case State.MoveRight:
                if (eyesContainer != null)
                    eyesContainer.localPosition = new Vector3(eyeShiftAmount, 0f, 0f);
                break;
            case State.Success:
                break;
            case State.Crashed:
                SetActive(exclamation, true);
                break;
        }
    }

    private void UpdateQuestionColors()
    {
        if (questionImages == null || thinkingColors == null || thinkingColors.Length == 0) return;

        for (var i = 0; i < questionImages.Length; i++)
        {
            if (questionImages[i] == null) continue;
            questionImages[i].color = thinkingColors[(i + thinkingOffset) % thinkingColors.Length];
        }
    }

    private static void SetActive(Component c, bool value)
    {
        if (c != null) c.gameObject.SetActive(value);
    }
}
