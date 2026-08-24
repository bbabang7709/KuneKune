using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 더위 게이지 UI의 색, 크기, 위치를 움직여 현재 위험 단계를 전달합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class HeatGaugeEffect : MonoBehaviour
{
    // 효과 임계값
    private const float PULSE_START = 0.5f;     // 맥박 효과 시작%
    private const float HOT_START = 0.72f;
    private const float DANGER_START = 0.9f;    // 흔들림 효과 시작%

    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Image outlineImage;
    [SerializeField] private TMP_Text labelText;

    [Header("Heat Colors")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _warmColor = new(1f, 0.72f, 0.24f, 1f);   // 노랑
    [SerializeField] private Color _hotColor = new(1f, 0.27f, 0.06f, 1f);    // 주황
    [SerializeField] private Color _dangerColor = new(1f, 0.05f, 0.02f, 1f); // 빨강
    [SerializeField] private Color _dangerLabelColor = new(1f, 0.55f, 0.4f, 1f);

    [Header("맥박 효과")]
    [SerializeField, Range(0f, 0.1f)] private float _maxPulseScale = 0.035f;
    [SerializeField, Range(0f, 10f)] private float _maxShakeDistance = 2.4f;

    private RectTransform _gaugeRoot;
    private Vector2 _originalPosition;
    private Vector3 _originalScale;
    private Color _originalOutlineColor;

    private Color _normalLabelColor;
    private Color _currentLabelColor;

    private Color _currentFillColor;
    
    private float _heatRatio;

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        _gaugeRoot = (RectTransform)transform;
        _originalPosition = _gaugeRoot.anchoredPosition;
        _originalScale = _gaugeRoot.localScale;

        _originalOutlineColor = outlineImage != null ? outlineImage.color : Color.white;
        _normalLabelColor = labelText != null ? labelText.color : Color.white;

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
        float motionStrength = Mathf.InverseLerp(PULSE_START, 1f, _heatRatio);
        if (motionStrength <= 0f)   // 게이지가 PULSE_START(50%)미만이면 효과 없음
        {
            RestoreTransform();
            ApplyBaseColors();
            return;
        }

        float time = Time.unscaledTime;

        // 맥박 -> 크기 변화
        float pulseSpeed = Mathf.Lerp(0.8f, 2.4f, motionStrength);
        float pulse = Mathf.Sin(time * pulseSpeed * Mathf.PI * 2f) * 0.5f + 0.5f;   // 0 ~ 1
        float scale = 1f + pulse * _maxPulseScale * motionStrength;
        _gaugeRoot.localScale = Vector3.Scale(_originalScale, new Vector3(scale, scale, 1f));

        // 90%부터 게이지 전체가 불규칙하게 조금씩 흔들립니다.
        float dangerStrength = Mathf.InverseLerp(DANGER_START, 1f, _heatRatio);
        float shakeX = (Mathf.Sin(time * 31.7f) + Mathf.Sin(time * 17.1f)) * 0.5f;
        float shakeY = (Mathf.Sin(time * 27.3f) + Mathf.Sin(time * 13.9f)) * 0.5f;
        Vector2 shake = new Vector2(shakeX, shakeY) * _maxShakeDistance * dangerStrength;
        _gaugeRoot.anchoredPosition = _originalPosition + shake;

        // 맥박이 커지는 순간 게이지도 함께 밝아집니다.
        ApplyAnimatedColors(pulse, motionStrength, dangerStrength);
    }

    /// <summary>
    /// 더위 게이지 비율에 맞춰 연출 단계를 갱신합니다.
    /// </summary>
    public void SetHeatRatio(float ratio)
    {
        _heatRatio = Mathf.Clamp01(ratio);
        _currentFillColor = EvaluateFillColor(_heatRatio);

        float hotStrength = Mathf.InverseLerp(HOT_START, DANGER_START, _heatRatio);
        _currentLabelColor = Color.Lerp(_normalLabelColor, _dangerLabelColor, hotStrength);

        ApplyBaseColors();
    }

    private Color EvaluateFillColor(float ratio)
    {
        // _normalColor -> _warmColor
        if (ratio < PULSE_START)
            return Color.Lerp(_normalColor, _warmColor, ratio / PULSE_START);

        // _warmColor -> _hotColor
        if (ratio < HOT_START)
            return Color.Lerp(_warmColor, _hotColor, Mathf.InverseLerp(PULSE_START, HOT_START, ratio));

        // _hotColor -> _dangerColor
        return Color.Lerp(_hotColor, _dangerColor, Mathf.InverseLerp(HOT_START, 1f, ratio));
    }

    private void ApplyAnimatedColors(float pulse, float motionStrength, float dangerStrength)
    {
        float fillBrightness = 1f + pulse * motionStrength * 0.14f;
        float frameBrightness = 1f + pulse * dangerStrength * 0.06f;

        fillImage.color = MultiplyBrightness(_currentFillColor, fillBrightness);
        outlineImage.color = _originalOutlineColor;
        labelText.color = MultiplyBrightness(_currentLabelColor, frameBrightness);
    }

    private void ApplyBaseColors()
    {
        fillImage.color = _currentFillColor;
        outlineImage.color = _originalOutlineColor;
        labelText.color = _currentLabelColor;
    }

    private void RestoreTransform()
    {
        _gaugeRoot.anchoredPosition = _originalPosition;
        _gaugeRoot.localScale = _originalScale;
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
