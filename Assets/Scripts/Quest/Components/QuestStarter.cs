using UnityEngine;

[DisallowMultipleComponent]
public class QuestStarter : MonoBehaviour
{
    [Header("Quest")]
    [Tooltip("시작할 퀘스트 데이터입니다.")]
    [SerializeField] private QuestData _questData;

    [Tooltip("이 오브젝트가 처음 시작될 때 퀘스트를 자동으로 시작합니다.")]
    [SerializeField] private bool _startOnStart = true;

    private void Start()
    {
        if (_startOnStart)
            StartQuest();
    }

    /// <summary>
    /// Inspector에 연결한 퀘스트를 시작합니다.
    /// </summary>
    public void StartQuest()
    {
        TryStartQuest();
    }

    /// <summary>
    /// Inspector에 연결한 퀘스트를 시작하고 성공 여부를 반환합니다.
    /// </summary>
    public bool TryStartQuest()
    {
        if (_questData == null)
        {
            Debug.LogWarning($"[{nameof(QuestStarter)}] 시작할 퀘스트 데이터가 연결되지 않았습니다.", this);
            return false;
        }

        if (QuestManager.Instance == null)
        {
            Debug.LogWarning($"[{nameof(QuestStarter)}] 장면에서 QuestManager를 찾을 수 없습니다.", this);
            return false;
        }

        return QuestManager.Instance.StartQuest(_questData.QuestID);
    }
}
