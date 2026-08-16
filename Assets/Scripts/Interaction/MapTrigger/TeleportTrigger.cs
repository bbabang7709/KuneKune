using Unity.Cinemachine;
using UnityEngine;

public class TeleportTrigger : MonoBehaviour
{
    [Header("Lock Enter")]
    [SerializeField] private bool canEnter = true;

    [Header("Link Settings")]
    [SerializeField] private Transform targetPoint = null;
    [SerializeField] private CinemachineCamera targetCamera;
    private BoxCollider2D boxCollider;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();

        if (canEnter)
        {
            boxCollider.isTrigger = true;
        }
        else
        {
            boxCollider.isTrigger = false;
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // TODO: 주인공 독백 텍스트 연출 추가
            Debug.Log("이 쪽이 아니야.");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (targetPoint == null) return;

        if (collision.CompareTag("Player"))
        {
            // TODO: 화면 페이드 인/아웃 연출 구현
            collision.transform.position = targetPoint.transform.position;

            if (targetCamera != null) CameraManager.Instance?.SetCamera(targetCamera);
        }
    }
}
