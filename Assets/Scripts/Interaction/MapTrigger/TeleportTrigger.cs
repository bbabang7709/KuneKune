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

#if UNITY_EDITOR
    [Header("Editor Test")]
    [Tooltip("이 통로로 추격 맵에 들어가면 해당 씬에서 진엔딩 조건을 충족한 것으로 테스트합니다.")]
    [SerializeField] private bool unlockTrueEndingForChaseTest;
#endif

    private BoxCollider2D boxCollider;

    public event System.Action<TeleportTrigger> PlayerEntered;

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
            // 맵 전환이 시작되기 전에 포털 도착 컷씬을 정리할 수 있게 알립니다.
            if (canEnter && targetMap != null && targetEntryPoint != null && MapManager.Instance != null)
                PlayerEntered?.Invoke(this);

#if UNITY_EDITOR
            if (unlockTrueEndingForChaseTest && canEnter && targetMap != null && targetEntryPoint != null &&
                targetMap.mapType == EMapType.Chase_Ricefield && MapManager.Instance != null &&
                SaveManager.Instance != null && !SaveManager.Instance.IsLoading)
            {
                SaveManager.Instance.EnableTrueEndingChaseTest(this);
                MapManager.Instance.ChangeMap(targetMap.mapType, targetEntryPoint.EntryID,
                    () => BackgroundManager.Instance?.SetBackgroundForChapter(EChapter.Chapter5_Chase));
                return;
            }
#endif
            // 화면 페이드 인/아웃 연출은 MapManager가 대신 수행합니다.
            ExecuteTransition();
        }
    }
}
