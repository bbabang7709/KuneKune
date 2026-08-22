using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 더위 게이지의 색, 크기, 위치를 움직여 현재 위험 단계를 전달합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class HeatGaugeEffect : MonoBehaviour
{
    private const float PulseStart = 0.5f;
    private const float HotStart = 0.72f;
    private const float DangerStart = 0.9f;

    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Image outlineImage;
    [SerializeField] private TMP_Text labelText;

    [Header("Heat Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warmColor = new(1f, 0.72f, 0.24f, 1f);
    [SerializeField] private Color hotColor = new(1f, 0.27f, 0.06f, 1f);
    [SerializeField] private Color dangerColor = new(1f, 0.05f, 0.02f, 1f);
    [SerializeField] private Color dangerLabelColor = new(1f, 0.55f, 0.4f, 1f);

    [Header("Animation")]
    [SerializeField, Range(0f, 0.1f)] private float maxPulseScale = 0.035f;
    [SerializeField, Range(0f, 10f)] private float maxShakeDistance = 2.4f;

    private RectTransform gaugeRoot;
    private Vector2 restingPosition;
    private Vector3 restingScale;
    private Color originalOutlineColor;
    private Color normalLabelColor;
    private Color currentFillColor;
    private Color currentLabelColor;
    private float heatRatio;

    private void Awake()
    {
        gaugeRoot = (RectTransform)transform;
        restingPosition = gaugeRoot.anchoredPosition;
        restingScale = gaugeRoot.localScale;

        originalOutlineColor = outlineImage != null ? outlineImage.color : Color.white;
        normalLabelColor = labelText != null ? labelText.color : Color.white;

        SetHeatRatio(0f);
    }

    private void OnEnable()
    {
        ApplyBaseColors();
    }

    private void OnDisable()
    {
        RestoreTransform();
    }

    private void Update()
    {
        float motionStrength = Mathf.InverseLerp(PulseStart, 1f, heatRatio);
        if (motionStrength <= 0f)
        {
            RestoreTransform();
            ApplyBaseColors();
            return;
        }

        float time = Time.unscaledTime;
        float pulseSpeed = Mathf.Lerp(0.8f, 2.4f, motionStrength);
        float pulse = Mathf.Sin(time * pulseSpeed * Mathf.PI * 2f) * 0.5f + 0.5f;

        float scale = 1f + pulse * maxPulseScale * motionStrength;
        gaugeRoot.localScale = Vector3.Scale(restingScale, new Vector3(scale, scale, 1f));

        // 90%부터 게이지 전체가 불규칙하게 조금씩 흔들립니다.
        float dangerStrength = Mathf.InverseLerp(DangerStart, 1f, heatRatio);
        float shakeX = (Mathf.Sin(time * 31.7f) + Mathf.Sin(time * 17.1f)) * 0.5f;
        float shakeY = (Mathf.Sin(time * 27.3f) + Mathf.Sin(time * 13.9f)) * 0.5f;
        Vector2 shake = new Vector2(shakeX, shakeY) * maxShakeDistance * dangerStrength;
        gaugeRoot.anchoredPosition = restingPosition + shake;

        // 맥박이 커지는 순간 게이지도 함께 밝아집니다.
        ApplyAnimatedColors(pulse, motionStrength, dangerStrength);
    }

    /// <summary>
    /// 더위 게이지 비율에 맞춰 연출 단계를 갱신합니다.
    /// </summary>
    public void SetHeatRatio(float ratio)
    {
        heatRatio = Mathf.Clamp01(ratio);
        currentFillColor = EvaluateFillColor(heatRatio);

        float hotStrength = Mathf.InverseLerp(HotStart, DangerStart, heatRatio);
        currentLabelColor = Color.Lerp(normalLabelColor, dangerLabelColor, hotStrength);

        ApplyBaseColors();
    }

    private Color EvaluateFillColor(float ratio)
    {
        if (ratio < PulseStart)
            return Color.Lerp(normalColor, warmColor, ratio / PulseStart);

        if (ratio < HotStart)
            return Color.Lerp(warmColor, hotColor, Mathf.InverseLerp(PulseStart, HotStart, ratio));

        return Color.Lerp(hotColor, dangerColor, Mathf.InverseLerp(HotStart, 1f, ratio));
    }

    private void ApplyAnimatedColors(float pulse, float motionStrength, float dangerStrength)
    {
        float fillBrightness = 1f + pulse * motionStrength * 0.14f;
        float frameBrightness = 1f + pulse * dangerStrength * 0.06f;

        if (fillImage != null)
            fillImage.color = MultiplyBrightness(currentFillColor, fillBrightness);
        if (outlineImage != null)
            outlineImage.color = originalOutlineColor;
        if (labelText != null)
            labelText.color = MultiplyBrightness(currentLabelColor, frameBrightness);
    }

    private void ApplyBaseColors()
    {
        if (fillImage != null)
            fillImage.color = currentFillColor;
        if (outlineImage != null)
            outlineImage.color = originalOutlineColor;
        if (labelText != null)
            labelText.color = currentLabelColor;
    }

    private void RestoreTransform()
    {
        if (gaugeRoot == null)
            return;

        gaugeRoot.anchoredPosition = restingPosition;
        gaugeRoot.localScale = restingScale;
    }

    private static Color MultiplyBrightness(Color color, float multiplier)
    {
        return new Color(
            Mathf.Clamp01(color.r * multiplier),
            Mathf.Clamp01(color.g * multiplier),
            Mathf.Clamp01(color.b * multiplier),
            color.a
        );
    }
}
