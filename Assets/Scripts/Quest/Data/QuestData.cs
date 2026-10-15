using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "KuneKune/Quest/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("Quest Info")]
    [Tooltip("퀘스트를 구분하는 고유한 ID입니다.")]
    [SerializeField] private string questID;

    [Tooltip("플레이어에게 보여줄 퀘스트 이름입니다.")]
    [SerializeField] private string questName;

    [Header("Completion Objectives")]
    [Tooltip("이 퀘스트를 완료하기 위해 끝내야 하는 대상의 ID 목록입니다.")]
    [SerializeField] private List<string> objectiveIDs = new();

    /// <summary>
    /// 퀘스트를 구분하는 고유한 ID입니다.
    /// 예: house_cleanup
    /// </summary>
    public string QuestID => questID;

    /// <summary>
    /// 플레이어에게 보여줄 퀘스트 이름입니다.
    /// 이름이 비어 있으면 퀘스트 ID를 대신 반환합니다.
    /// </summary>
    public string QuestName => string.IsNullOrWhiteSpace(questName) ? questID : questName;

    /// <summary>
    /// 퀘스트를 완료하기 위해 끝내야 하는 대상의 ID 목록입니다.
    /// 외부 코드에서는 목록의 내용을 변경할 수 없습니다.
    /// </summary>
    public IReadOnlyList<string> ObjectiveIDs => objectiveIDs;

    /// <summary>
    /// 퀘스트를 완료하기 위해 끝내야 하는 대상의 전체 개수입니다.
    /// </summary>
    public int ObjectiveCount => objectiveIDs.Count;

    /// <summary>
    /// 대상 ID가 이 퀘스트에 포함되어 있는지 확인합니다.
    /// </summary>
    public bool ContainsObjective(string objectiveID)
    {
        if (string.IsNullOrWhiteSpace(objectiveID))
            return false;

        return objectiveIDs.Contains(objectiveID);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Unity 편집기에서 같은 대상 ID를 두 번 입력했는지 확인합니다.
    /// </summary>
    private void OnValidate()
    {
        HashSet<string> foundIDs = new();

        foreach (string objectiveID in objectiveIDs)
        {
            if (string.IsNullOrWhiteSpace(objectiveID))
                continue;

            if (foundIDs.Add(objectiveID) == false)
                Debug.LogWarning($"[{nameof(QuestData)}] 같은 대상 ID가 두 번 입력되었습니다: {objectiveID}", this);
        }
    }
#endif
}
