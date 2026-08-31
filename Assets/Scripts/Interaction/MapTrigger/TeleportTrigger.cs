using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 특별한 조작 없이 닿기만 하면 맵 전환이 일어나는 Object
/// </summary>
public class TeleportTrigger : MapTransition
{
    [Header("Lock Enter")]
    [SerializeField] private bool canEnter = true;  // 지나갈 수 있는지 여부
    private BoxCollider2D boxCollider;

    public override bool CanKuneKuneUse => canEnter;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();

        if (canEnter) // 지나갈 수 있으면 Trigger Collider (텔레포트)
        {
            boxCollider.isTrigger = true;
        }
        else    // 지나갈 수 없으면 일반 Collider (가로막기)
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
        if (collision.CompareTag("Player"))
        {
            // TODO: 화면 페이드 인/아웃 연출 구현
            ExecuteTransition();
        }
    }
}
