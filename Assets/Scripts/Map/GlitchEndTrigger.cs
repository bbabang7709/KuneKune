using UnityEngine;

/// <summary>
/// 회상 구간의 글리치 종료 트리거. 닿으면 글리치를 서서히 끄고, 선택적으로 더위 게이지를 되돌린다.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class GlitchEndTrigger : MonoBehaviour
{
    [Header("Glitch")]
    [Tooltip("글리치가 완전히 사라지는 데 걸리는 시간")]
    [SerializeField] private float fadeOutDuration = 1.5f;

    [Header("Heat (선택)")]
    [SerializeField] private bool resetHeat = false;
    [Tooltip("글리치 종료 시 서서히 낮출 더위 게이지 목표값")]
    [SerializeField] private float resetHeatTo = 0f;

    private bool _hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_hasTriggered || !collision.CompareTag("Player")) return;

        _hasTriggered = true;

        GlitchEffectController.Instance?.RampTo(0f, fadeOutDuration);

        if (resetHeat)
        {
            PlayerMovement pm = collision.GetComponent<PlayerMovement>();
            if (pm != null) pm.SetCurrentHeat(resetHeatTo);
        }

        GetComponent<Collider2D>().enabled = false;
    }
}
