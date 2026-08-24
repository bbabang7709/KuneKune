using System.Collections.Generic;
using UnityEngine;

public enum EQuestStatus
{
    NotStarted,
    InProgress,
    Completed
}

/// <summary>
/// 퀘스트 진행 상태를 기록합니다.
/// </summary>
public sealed class QuestProgress
{
    private readonly QuestData _data;
    private readonly HashSet<string> completedObjectiveIDs = new();

    public QuestData Data => _data;

    public EQuestStatus Status { get; private set; } = EQuestStatus.NotStarted;

    public int CompletedCount => completedObjectiveIDs.Count;
    public int ObjectiveCount => _data.ObjectiveCount;

    /// <summary>
    /// 퀘스트가 완료되었는지 알려줍니다.
    /// </summary>
    public bool IsCompleted => Status == EQuestStatus.Completed;

    public QuestProgress(QuestData data)
    {
        if (data == null)
        {
            Debug.LogError("null 데이터가 세상에 어딨냐");
            return;
        }
        _data = data;
    }

    /// <summary>
    /// 시작 전인 퀘스트를 진행 중 상태로 변경합니다.
    /// 완료할 대상이 없거나 이미 시작한 퀘스트라면 false를 반환합니다.
    /// </summary>
    public bool Start()
    {
        if (Status != EQuestStatus.NotStarted || ObjectiveCount <= 0)
            return false;

        Status = EQuestStatus.InProgress;
        UpdateCompletionStatus();
        return true;
    }

    /// <summary>
    /// 퀘스트 대상 하나를 완료했다고 기록합니다. 중복 기록은 방지합니다.
    /// </summary>
    public bool TryCompleteObjective(string objectiveID)
    {
        if (Status != EQuestStatus.InProgress)
            return false;

        if (_data.ContainsObjective(objectiveID) == false)
            return false;

        if (IsObjectiveCompleted(objectiveID))
            return false;

        completedObjectiveIDs.Add(objectiveID);
        UpdateCompletionStatus();
        return true;
    }

    /// <summary>
    /// 퀘스트 대상이 이미 완료되었는지 확인합니다.
    /// </summary>
    public bool IsObjectiveCompleted(string objectiveID)
    {
        if (string.IsNullOrWhiteSpace(objectiveID))
            return false;

        return completedObjectiveIDs.Contains(objectiveID);
    }

    /// <summary>
    /// 필요한 대상을 모두 완료했다면 퀘스트를 완료 상태로 변경합니다.
    /// </summary>
    private void UpdateCompletionStatus()
    {
        if (CompletedCount < ObjectiveCount)
            return;

        Status = EQuestStatus.Completed;
    }
}