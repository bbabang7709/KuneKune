using UnityEngine;

[DisallowMultipleComponent]
public class QuestObjectiveReporter : MonoBehaviour
{
    [Header("Quest Objective")]
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

    /// <summary>
    /// 퀘스트 대상이 완료되었다고 보고합니다.
    /// </summary>
    public void ReportCompleted()
    {
        TryReportCompleted();
    }

    /// <summary>
    /// 퀘스트 대상이 완료되었다고 보고하고 성공 여부를 반환합니다.
    /// </summary>
    public bool TryReportCompleted()
    {
        if (_hasReported)
            return false;

        if (_questData == null)
        {
            Debug.LogWarning($"[{nameof(QuestObjectiveReporter)}] 퀘스트 데이터가 연결되지 않았습니다.", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_objectiveID))
        {
            Debug.LogWarning($"[{nameof(QuestObjectiveReporter)}] 대상 ID가 비어 있습니다.", this);
            return false;
        }

        if (_questData.ContainsObjective(_objectiveID) == false)
        {
            Debug.LogWarning($"[{nameof(QuestObjectiveReporter)}] 퀘스트에 없는 대상 ID입니다: {_objectiveID}", this);
            return false;
        }

        if (QuestManager.Instance.IsObjectiveCompleted(_questData.QuestID, _objectiveID))
        {
            _hasReported = true;
            return false;
        }

        bool reportSucceeded = QuestManager.Instance.ReportObjectiveCompleted(_questData.QuestID, _objectiveID);
        _hasReported = reportSucceeded;
        return reportSucceeded;
    }
}
