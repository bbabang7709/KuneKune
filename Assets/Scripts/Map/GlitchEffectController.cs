using System.Collections;
using UnityEngine;

/// <summary>
/// 회상 구간용 전체 화면 글리치를 관리한다.
/// KuneKune와 같은 머티리얼의 _Intensity를 목표값까지 서서히 변경하고, _GlitchTime을 갱신한다.
/// 씬에 하나만 두고 GlitchZoneTrigger / GlitchEndTrigger가 호출한다.
/// </summary>
public class GlitchEffectController : MonoBehaviour
{
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int GlitchTimeId = Shader.PropertyToID("_GlitchTime");

    public static GlitchEffectController Instance { get; private set; }

    [Tooltip("KuneKune의 Full Screen Glitch와 같은 머티리얼")]
    [SerializeField] private Material glitchMaterial;

    private float _currentIntensity;
    private Coroutine _rampRoutine;

    public float CurrentIntensity => _currentIntensity;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDisable()
    {
        // 공유 머티리얼 에셋에 값이 남지 않도록 복구한다.
        _currentIntensity = 0f;
        if (glitchMaterial != null) glitchMaterial.SetFloat(IntensityId, 0f);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void LateUpdate()
    {
        // 쿠네쿠네가 비활성이어도, 메뉴로 시간이 멈춰도 패턴이 움직이도록 직접 갱신한다.
        if (glitchMaterial != null && _currentIntensity > 0f)
            glitchMaterial.SetFloat(GlitchTimeId, Time.unscaledTime);
    }

    /// <summary>현재 강도에서 target까지 duration초에 걸쳐 변경한다.</summary>
    public void RampTo(float target, float duration)
    {
        if (glitchMaterial == null) return;

        if (_rampRoutine != null) StopCoroutine(_rampRoutine);
        if (duration <= 0f)
        {
            _rampRoutine = null;
            Apply(Mathf.Clamp01(target));
            return;
        }

        _rampRoutine = StartCoroutine(RampRoutine(Mathf.Clamp01(target), Mathf.Max(0f, duration)));
    }

    private IEnumerator RampRoutine(float target, float duration)
    {
        float start = _currentIntensity;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            Apply(Mathf.Lerp(start, target, elapsed / duration));
            yield return null;
        }

        Apply(target);
        _rampRoutine = null;
    }

    private void Apply(float value)
    {
        _currentIntensity = value;
        glitchMaterial.SetFloat(IntensityId, value);
    }
}
