using UnityEngine;

[DisallowMultipleComponent]
public class QuestObjective : MonoBehaviour
{
    [Header("인스펙터에서 직접 설정")]
    [Tooltip("완료 사실을 보고할 퀘스트 데이터입니다.")]
    [SerializeField] private QuestData _questData;
    public QuestData QuestData => _questData;

    [Tooltip("이 오브젝트가 완료할 퀘스트 대상의 ID입니다.")]
    [SerializeField] private string _objectiveID;

    private bool _hasReported;
    /// <summary>
    /// 이 컴포넌트가 완료 사실을 정상적으로 보고했는지 알려줍니다.
    /// </summary>
    public bool HasReported => _hasReported;

    protected virtual void Start()
    {
        Init();

        // SaveManager의 Global Flag를 통해 이 목표가 이미 달성됐는지 확인
        string flagKey = $"QuestBoj_{_objectiveID}";
        if (SaveManager.Instance != null && SaveManager.Instance.GetFlag(flagKey))
        {
            _hasReported = true;
            Destroy(gameObject);    // 이미 완료된 퀘스트 오브젝트는 파괴
        }
    }

    protected virtual void Init()
    {
        if (string.IsNullOrWhiteSpace(_objectiveID))
        {
            //Debug.Log($"[{nameof(QuestObjective)}] 대상 ID가 비어 있어서 오브젝트 이름으로 대체했습니다.", this);
            _objectiveID = gameObject.name;
        }
    }

    /// <summary>
    /// 퀘스트 대상이 완료되었다고 보고하고 성공 여부를 반환합니다.
    /// </summary>
    public bool TryReportCompleted()
    {
        if (_hasReported)
            return false;
        
        bool reportSucceeded = QuestManager.Instance.ReportObjectiveCompleted(_questData.QuestID, _objectiveID);
        _hasReported = reportSucceeded;

        // 보고 성공 시 플래그에 기록
        if (reportSucceeded)
        {
            SaveManager.Instance?.SetFlag($"QuestObj_{_objectiveID}", true);
        }

        return reportSucceeded;
    }
}
