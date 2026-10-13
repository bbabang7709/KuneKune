using UnityEngine;

/// <summary>
/// 회상 구간의 글리치 단계 트리거. 여러 개를 배치해 강도를 단계적으로 높인다.
/// 닿으면 글리치 강도를 목표값까지 올리고, 선택적으로 더위 게이지 목표도 함께 설정한다.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class GlitchZoneTrigger : MonoBehaviour
{
    [Header("Glitch")]
    [SerializeField, Range(0f, 1f)] private float targetIntensity = 0.3f;
    [Tooltip("목표 강도까지 걸리는 시간")]
    [SerializeField] private float rampDuration = 2f;

    [Header("Heat (선택)")]
    [SerializeField] private bool applyHeat = false;
    [Tooltip("이 구간에서 서서히 올릴 더위 게이지 목표값. 최대치 도달 시 쿠네쿠네가 등장하므로 최대치보다 낮게 설정한다.")]
    [SerializeField] private float targetHeat = 40f;

    private bool _hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_hasTriggered || !collision.CompareTag("Player")) return;

        _hasTriggered = true;

        GlitchEffectController.Instance?.RampTo(targetIntensity, rampDuration);

        if (applyHeat)
        {
            PlayerMovement pm = collision.GetComponent<PlayerMovement>();
            if (pm != null) pm.SetCurrentHeat(Mathf.Min(targetHeat, pm.MaxHeat - 1f));
        }

        GetComponent<Collider2D>().enabled = false;
    }
}
