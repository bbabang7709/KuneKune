using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Quest Data")]
    [Tooltip("게임에서 사용할 퀘스트 데이터를 등록합니다.")]
    [SerializeField] private List<QuestData> _questDataList = new();

    private readonly Dictionary<string, QuestProgress> _questProgresses = new(StringComparer.Ordinal);

    /// <summary>
    /// 퀘스트를 시작했을 때 퀘스트 ID를 전달합니다.
    /// </summary>
    public event Action<string> OnQuestStarted;

    /// <summary>
    /// 진행 수가 바뀌었을 때 퀘스트 ID, 완료한 수, 전체 수를 전달합니다.
    /// </summary>
    public event Action<string, int, int> OnQuestProgressChanged;

    /// <summary>
    /// 퀘스트를 완료했을 때 퀘스트 ID를 전달합니다.
    /// </summary>
    public event Action<string> OnQuestCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CreateQuestProgresses();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static QuestManager Instantiate()
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/Etc/QuestManager");
        QuestManager q = Instantiate(prefab).GetComponent<QuestManager>();
        return q;
    }

    /// <summary>
    /// Inspector에 등록한 데이터마다 실행 중 진행 상태를 만듭니다.
    /// </summary>
    private void CreateQuestProgresses()
    {
        _questProgresses.Clear();

        foreach (QuestData questData in _questDataList)
        {
            if (questData == null)
            {
                Debug.LogWarning($"[{nameof(QuestManager)}] 비어 있는 퀘스트 데이터가 있습니다.", this);
                continue;
            }

            if (string.IsNullOrWhiteSpace(questData.QuestID))
            {
                Debug.LogWarning($"[{nameof(QuestManager)}] ID가 비어 있는 퀘스트 데이터가 있습니다: {questData.name}", questData);
                continue;
            }

            if (_questProgresses.ContainsKey(questData.QuestID))
            {
                Debug.LogWarning($"[{nameof(QuestManager)}] 같은 퀘스트 ID가 두 번 등록되었습니다: {questData.QuestID}", questData);
                continue;
            }

            _questProgresses.Add(questData.QuestID, new QuestProgress(questData));
        }
    }

    /// <summary>
    /// 등록된 퀘스트를 시작합니다.
    /// </summary>
    public bool StartQuest(string questID)
    {
        if (TryGetQuestProgress(questID, out QuestProgress progress) == false)
        {
            Debug.LogWarning($"[{nameof(QuestManager)}] 등록되지 않은 퀘스트입니다: {questID}", this);
            return false;
        }

        if (progress.Start() == false)
        {
            if (progress.ObjectiveCount <= 0)
                Debug.LogWarning($"[{nameof(QuestManager)}] 완료할 대상이 없어 퀘스트를 시작할 수 없습니다: {questID}", progress.Data);

            return false;
        }

        Debug.Log($"[Quest] 퀘스트 시작: {questID}", this);

        OnQuestStarted?.Invoke(questID);
        OnQuestProgressChanged?.Invoke(questID, progress.CompletedCount, progress.ObjectiveCount);
        return true;
    }

    /// <summary>
    /// 퀘스트 대상 하나를 완료했다고 보고합니다.
    /// 완료된 대상은 보고를 실패합니다. (중복 방지)
    /// </summary>
    public bool ReportObjectiveCompleted(string questID, string objectiveID)
    {
        if (TryGetQuestProgress(questID, out QuestProgress progress) == false)
        {
            Debug.LogWarning($"[{nameof(QuestManager)}] 등록되지 않은 퀘스트입니다: {questID}", this);
            return false;
        }

        if (progress.TryCompleteObjective(objectiveID) == false)
            return false;

        Debug.Log($"[Quest] 진행: {questID} / {progress.CompletedCount} / {progress.ObjectiveCount}", this);
        OnQuestProgressChanged?.Invoke(questID, progress.CompletedCount, progress.ObjectiveCount);

        if (progress.IsCompleted)
        {
            Debug.Log($"[Quest] 퀘스트 완료: {questID}", this);
            OnQuestCompleted?.Invoke(questID);
        }

        return true;
    }

    #region Util
    /// <summary>
    /// 퀘스트가 현재 진행 중인지 확인합니다.
    /// </summary>
    public bool IsQuestInProgress(string questID)
    {
        return TryGetQuestProgress(questID, out QuestProgress progress) && progress.Status == EQuestStatus.InProgress;
    }

    /// <summary>
    /// 퀘스트가 완료되었는지 확인합니다.
    /// </summary>
    public bool IsQuestCompleted(string questID)
    {
        return TryGetQuestProgress(questID, out QuestProgress progress) && progress.IsCompleted;
    }

    /// <summary>
    /// 퀘스트 대상 하나가 완료되었는지 확인합니다.
    /// </summary>
    public bool IsObjectiveCompleted(string questID, string objectiveID)
    {
        return TryGetQuestProgress(questID, out QuestProgress progress) && progress.IsObjectiveCompleted(objectiveID);
    }

    /// <summary>
    /// 전달받은 퀘스트 대상이 모두 완료되었는지 확인합니다.
    /// </summary>
    public bool AreObjectivesCompleted(string questID, params string[] objectiveIDs)
    {
        if (TryGetQuestProgress(questID, out QuestProgress progress) == false)
            return false;

        if (objectiveIDs == null || objectiveIDs.Length == 0)
            return false;

        foreach (string id in objectiveIDs)
        {
            if (progress.IsObjectiveCompleted(id) == false)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 현재까지 완료한 대상의 개수를 반환합니다.
    /// 등록되지 않은 퀘스트라면 0을 반환합니다.
    /// </summary>
    public int GetCompletedCount(string questID)
    {
        return TryGetQuestProgress(questID, out QuestProgress progress) ? progress.CompletedCount : 0;
    }

    /// <summary>
    /// 완료해야 하는 대상의 전체 개수를 반환합니다.
    /// 등록되지 않은 퀘스트라면 0을 반환합니다.
    /// </summary>
    public int GetObjectiveCount(string questID)
    {
        return TryGetQuestProgress(questID, out QuestProgress progress) ? progress.ObjectiveCount : 0;
    }

    private bool TryGetQuestProgress(string questID, out QuestProgress progress)
    {
        if (string.IsNullOrWhiteSpace(questID))
        {
            progress = null;
            return false;
        }

        return _questProgresses.TryGetValue(questID, out progress);
    }
    #endregion
}
