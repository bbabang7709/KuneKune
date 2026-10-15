using UnityEngine;

/// <summary>
/// 더위 단계에 맞는 숨소리와 심장 소리의 재생을 사운드 매니저에 요청합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement))]
public sealed class HeatAudioEffect : MonoBehaviour
{
    [Header("사운드 데이터베이스 ID")]
    [SerializeField] private string warmBreathID = "Heat_Breath_Warm";
    [SerializeField] private string hotBreathID = "Heat_Breath_Hot";
    [SerializeField] private string dangerBreathID = "Heat_Breath_Danger";
    [SerializeField] private string heartbeatID = "Heat_Heartbeat";

    private PlayerMovement movement;
    private SoundManager soundManager;
    private AudioSource warmBreath;
    private AudioSource hotBreath;
    private AudioSource dangerBreath;
    private AudioSource heartbeat;
    private float heatRatio;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        movement.OnHeatChanged += OnHeatChanged;
        heatRatio = movement.HeatRatio;
    }

    private void OnDisable()
    {
        movement.OnHeatChanged -= OnHeatChanged;
        StopSounds();
    }

    private void OnHeatChanged(float current, float max)
    {
        heatRatio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
    }

    private void Update()
    {
        // 매니저가 플레이어보다 늦게 만들어지거나 교체되어도 다시 연결합니다.
        if (soundManager != SoundManager.Instance)
        {
            StopSounds();
            soundManager = SoundManager.Instance;
            if (soundManager != null)
            {
                warmBreath = soundManager.PlayLoopSFX(warmBreathID);
                hotBreath = soundManager.PlayLoopSFX(hotBreathID);
                dangerBreath = soundManager.PlayLoopSFX(dangerBreathID);
                heartbeat = soundManager.PlayLoopSFX(heartbeatID);
            }
        }
        if (soundManager == null)
            return;

        float breathVolume = EvaluateBreathVolume(heatRatio);
        // 단계가 바뀌는 첫 5% 구간에서는 두 숨소리를 조금씩 섞습니다.
        float hotBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.60f, 0.65f, heatRatio));
        float dangerBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.85f, 0.90f, heatRatio));
        soundManager.SetLoopSFX(warmBreath, breathVolume * (1f - hotBlend));
        soundManager.SetLoopSFX(hotBreath, breathVolume * hotBlend * (1f - dangerBlend));
        soundManager.SetLoopSFX(dangerBreath, breathVolume * dangerBlend);

        // 너무 느리게 재생하면 박동이 지나치게 낮아지므로 원래 속도의 80%까지만 낮춥니다.
        float heartSpeed = Mathf.Lerp(0.8f, 1f, Mathf.InverseLerp(0.85f, 1f, heatRatio));
        soundManager.SetLoopSFX(heartbeat, EvaluateHeartbeatVolume(heatRatio), heartSpeed);
    }

    internal static float EvaluateBreathVolume(float ratio)
    {
        if (ratio <= 0.30f)
            return Mathf.Lerp(0f, 0.025f, Mathf.InverseLerp(0f, 0.30f, ratio));
        if (ratio <= 0.60f)
            return Mathf.Lerp(0.025f, 0.28f, Mathf.InverseLerp(0.30f, 0.60f, ratio));
        if (ratio <= 0.85f)
            return Mathf.Lerp(0.28f, 0.65f, Mathf.InverseLerp(0.60f, 0.85f, ratio));
        return Mathf.Lerp(0.65f, 1f, Mathf.InverseLerp(0.85f, 1f, ratio));
    }

    internal static float EvaluateHeartbeatVolume(float ratio)
    {
        if (ratio <= 0.50f)
            return 0f;
        if (ratio <= 0.85f)
            return Mathf.Lerp(0f, 0.6f, Mathf.InverseLerp(0.50f, 0.85f, ratio));
        return Mathf.Lerp(0.6f, 1f, Mathf.InverseLerp(0.85f, 1f, ratio));
    }

    private void StopSounds()
    {
        if (soundManager != null)
        {
            soundManager.StopLoopSFX(warmBreath);
            soundManager.StopLoopSFX(hotBreath);
            soundManager.StopLoopSFX(dangerBreath);
            soundManager.StopLoopSFX(heartbeat);
        }
        soundManager = null;
        warmBreath = hotBreath = dangerBreath = heartbeat = null;
    }
}
