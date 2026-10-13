using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(BoxCollider2D))]
public class MapRevealTrigger : MonoBehaviour
{
    [Header("Reveal Settings")]
    [Tooltip("다음 맵이 밝아질 때 걸리는 시간")]
    [SerializeField] private float revealDuration = 2.0f;

    [Tooltip("다음 맵의 원래 글로벌 라이트 색상")]
    [SerializeField] private Color normalColor = Color.white;

    [Tooltip("다음 맵의 원래 글로벌 라이트 강도")]
    [SerializeField] private float normalIntensity = 1.0f;

    [Header("Player Light")]
    [SerializeField] private Light2D playerLight;

    [Header("Return Block (선택)")]
    [Tooltip("밝아지는 순간 활성화해 플레이어가 되돌아가지 못하게 막는 물리 벽 (BoxCollider2D)")]
    [SerializeField] private GameObject blockerWall;
    [Tooltip("더 이상 이 선 왼쪽으로 되돌아가지 못하게 제한할 월드 X 좌표 (빈 오브젝트)")]
    [SerializeField] private Transform leftLimitXPoint;

    private bool _hasTriggered = false;

    private void Awake()
    {
        if (blockerWall != null) blockerWall.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_hasTriggered || !collision.CompareTag("Player")) return;

        _hasTriggered = true;

        // // 칠흑 같았던 글로벌 라이트를 서서히 원래대로 복구한다.
        // if (LightingEventManager.Instance != null)
        // {
        //     LightingEventManager.Instance.FadeGlobalLight(normalColor, normalIntensity, revealDuration);
        // }

        // if (playerLight != null) playerLight.intensity = 0f;

        // // 물리적 차단: 플레이어 뒤를 막는 벽 활성화
        // if (blockerWall != null) blockerWall.SetActive(true);

        // // 논리적 차단: 플레이어 이동 스크립트에 한계선 X 좌표 주입
        // if (leftLimitXPoint != null)
        // {
        //     PlayerMovement pm = collision.GetComponent<PlayerMovement>();
        //     if (pm != null) pm.SetLeftMovementLimit(leftLimitXPoint.position.x);
        // }

        // // 벽이 이 오브젝트의 자식일 수 있으므로 오브젝트를 파괴하지 않고 트리거만 끈다.
        // enabled = false;
        // GetComponent<Collider2D>().enabled = false;

        StartCoroutine(TransitionRoutine(collision));
    }


    private IEnumerator TransitionRoutine(Collider2D collision)
    {
        // 칠흑 같았던 글로벌 라이트를 서서히 원래대로 복구한다.
        if (LightingEventManager.Instance != null)
        {
            LightingEventManager.Instance.FadeGlobalLight(normalColor, normalIntensity, revealDuration);
        }

        yield return new WaitForSeconds(revealDuration);

        if (playerLight != null) playerLight.intensity = 0f;

        // 물리적 차단: 플레이어 뒤를 막는 벽 활성화
        if (blockerWall != null) blockerWall.SetActive(true);

        // 논리적 차단: 플레이어 이동 스크립트에 한계선 X 좌표 주입
        if (leftLimitXPoint != null)
        {
            PlayerMovement pm = collision.GetComponent<PlayerMovement>();
            if (pm != null) pm.SetLeftMovementLimit(leftLimitXPoint.position.x);
        }

        // 벽이 이 오브젝트의 자식일 수 있으므로 오브젝트를 파괴하지 않고 트리거만 끈다.
        enabled = false;
        GetComponent<Collider2D>().enabled = false;
    }
}
