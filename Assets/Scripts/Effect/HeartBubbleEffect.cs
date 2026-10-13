using System.Collections;
using UnityEngine;

/// <summary>
/// 캐릭터 위에 하트 말풍선이 떠올라 위아래로 둥실거린 뒤 사라지는 연출.
/// 말풍선 오브젝트(SpriteRenderer 포함)에 붙이고 캐릭터의 자식으로 두면 캐릭터를 따라다닌다.
/// Play()를 호출하면 재생되고, 끝나면 오브젝트를 비활성화한다.
/// </summary>
public class HeartBubbleEffect : MonoBehaviour
{
    [Header("Rise (등장)")]
    [Tooltip("등장 시 떠오르는 거리")]
    [SerializeField] private float riseDistance = 0.4f;
    [SerializeField] private float riseDuration = 0.3f;

    [Header("Bob (둥실둥실)")]
    [Tooltip("위아래로 오르내리는 횟수")]
    [SerializeField, Min(1)] private int bobCount = 3;
    [Tooltip("한 번 오르내리는 데 걸리는 시간")]
    [SerializeField] private float bobDuration = 0.6f;
    [Tooltip("위아래 흔들림 폭")]
    [SerializeField] private float bobAmplitude = 0.08f;

    [Header("Fade (사라짐)")]
    [SerializeField] private float fadeDuration = 0.35f;

    [Header("Options")]
    [SerializeField] private bool playOnEnable = false;

    private SpriteRenderer[] _renderers;
    private Vector3 _baseLocalPosition;
    private Vector3 _baseLocalScale;
    private Coroutine _routine;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        _baseLocalPosition = transform.localPosition;
        _baseLocalScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (playOnEnable) Play();
    }

    private void OnDisable()
    {
        _routine = null;
        Restore();
    }

    /// <summary>연출을 재생한다. 재생 중이면 처음부터 다시 시작한다.</summary>
    public void Play()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        Vector3 startPos = _baseLocalPosition;
        Vector3 topPos = _baseLocalPosition + Vector3.up * riseDistance;

        SetAlpha(0f);
        transform.localPosition = startPos;
        transform.localScale = _baseLocalScale * 0.5f;

        // 1. 떠오르며 커지면서 나타남
        for (float t = 0f; t < riseDuration; t += Time.deltaTime)
        {
            float k = EaseOutCubic(t / riseDuration);
            transform.localPosition = Vector3.Lerp(startPos, topPos, k);
            transform.localScale = Vector3.Lerp(_baseLocalScale * 0.5f, _baseLocalScale, k);
            SetAlpha(k);
            yield return null;
        }

        transform.localPosition = topPos;
        transform.localScale = _baseLocalScale;
        SetAlpha(1f);

        // 2. 위아래로 둥실둥실 (사인파로 부드럽게)
        float bobTotal = bobDuration * bobCount;
        for (float t = 0f; t < bobTotal; t += Time.deltaTime)
        {
            float offset = Mathf.Sin(t / bobDuration * Mathf.PI * 2f) * bobAmplitude;
            transform.localPosition = topPos + Vector3.up * offset;
            yield return null;
        }

        transform.localPosition = topPos;

        // 3. 위로 살짝 올라가며 사라짐
        Vector3 endPos = topPos + Vector3.up * (riseDistance * 0.3f);
        for (float t = 0f; t < fadeDuration; t += Time.deltaTime)
        {
            float k = t / fadeDuration;
            transform.localPosition = Vector3.Lerp(topPos, endPos, k);
            SetAlpha(1f - k);
            yield return null;
        }

        _routine = null;
        gameObject.SetActive(false);   // OnDisable에서 원래 위치/스케일 복구
    }

    private void Restore()
    {
        transform.localPosition = _baseLocalPosition;
        transform.localScale = _baseLocalScale;
        SetAlpha(1f);
    }

    private void SetAlpha(float alpha)
    {
        if (_renderers == null) return;

        foreach (SpriteRenderer sr in _renderers)
        {
            if (sr == null) continue;
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    private static float EaseOutCubic(float x)
    {
        x = Mathf.Clamp01(x);
        return 1f - Mathf.Pow(1f - x, 3f);
    }
}
