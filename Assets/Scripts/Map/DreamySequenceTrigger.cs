using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 회상 구간 진입 시 글로벌 라이트를 어둡게 낮추는 트리거.
/// 맵/카메라 교체 없이 라이트 페이드로만 연출하며, 다시 밝히는 것은 MapRevealTrigger가 담당한다.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class DreamySequenceTrigger : MonoBehaviour
{
    [Header("Transition Lighting")]
    [Tooltip("어두워지는 데 걸리는 시간")]
    [SerializeField] private float darkFadeDuration = 1.5f;
    [Tooltip("어두워질 목표 글로벌 라이트 색상 (칠흑 같은 색)")]
    [SerializeField] private Color darkColor = new Color(0.02f, 0.02f, 0.05f, 1f);
    [SerializeField] private float darkIntensity = 0.05f;

    [Header("Player Light")]
    [SerializeField] private Light2D playerLight;

    private bool _hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_hasTriggered || !collision.CompareTag("Player")) return;

        _hasTriggered = true;

        // 글로벌 조명을 서서히 어둡게 바꾼다. (플레이어 전용 2D 라이트만 돋보임)
        if (LightingEventManager.Instance != null)
        {
            LightingEventManager.Instance.FadeGlobalLight(darkColor, darkIntensity, darkFadeDuration);
        }

        if (playerLight != null) playerLight.intensity = 1f;
    }
}
