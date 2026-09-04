using UnityEngine;

/// <summary>
/// 특별한 조작 없이 닿기만 하면 맵 전환이 일어나는 Object
/// </summary>
public class TeleportTrigger : MapTransition
{
    [Header("Lock Enter")]
    [SerializeField] private bool canEnter = true;  // 지나갈 수 있는지 여부
    
    [Tooltip("지나갈 수 없을 때 출력할 독백 대화 데이터")]
    [SerializeField] private DialogData cantEnterDialog; 

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
            // 독백 텍스트 연출 연결
            if (cantEnterDialog != null)
            {
                DialogSystem.Instance?.PlayDialog(cantEnterDialog);
            }
            else
            {
                Debug.Log("이 쪽이 아니야.");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 화면 페이드 인/아웃 연출은 MapManager가 대신 수행합니다.
            ExecuteTransition();
        }
    }
}