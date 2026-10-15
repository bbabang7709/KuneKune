using UnityEngine;
using TMPro;
using System.Collections;

public class QuestUI : UIBase
{
    [Header("References")]
    [Tooltip("퀘스트 UI 전체를 껐다 켤 때 사용할 Panel입니다.")]
    [SerializeField] private GameObject _questPanel;

    [Tooltip("퀘스트의 이름(또는 ID)을 표시할 텍스트입니다.")]
    [SerializeField] private TextMeshProUGUI _questNameText;

    [Tooltip("퀘스트 진행도(예: 1 / 3)를 표시할 텍스트입니다.")]
    [SerializeField] private TextMeshProUGUI _progressText;

    private string _currentTrackingQuestID;

    void Start()
    {
        HideUI(0f);

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted += HandleQuestStarted;
            QuestManager.Instance.OnQuestProgressChanged += HandleQuestProgressChanged;
            QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
        }
    }

    void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted -= HandleQuestStarted;
            QuestManager.Instance.OnQuestProgressChanged -= HandleQuestProgressChanged;
            QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
        }
    }

    private void HandleQuestStarted(string questID)
    {
        // 집 청소는 목표 안내와 진행도 UI를 표시하지 않습니다.
        if (questID == "HouseCleanup") return;

        _currentTrackingQuestID = questID;
        ShowUI();

        string questName = QuestManager.Instance.GetQuestName(questID);
        if (_questNameText != null)
        {
            _questNameText.text = $"목표: {questName}";
        }

        int completed = QuestManager.Instance.GetCompletedCount(questID);
        int total = QuestManager.Instance.GetObjectiveCount(questID);
        UpdateProgressText(completed, total);
    }

    private void HandleQuestProgressChanged(string questID, int completedCount, int objectiveCount)
    {
        if (questID != _currentTrackingQuestID) return;

        UpdateProgressText(completedCount, objectiveCount);
    }

    private void HandleQuestCompleted(string questID)
    {
        if (questID != _currentTrackingQuestID) return;

        StartCoroutine(QuestCompleteRoutine());
    }

    private IEnumerator QuestCompleteRoutine()
    {
        if (_questNameText != null) _questNameText.text = "목표 달성!";
        if (_progressText != null) _progressText.text = "";

        yield return new WaitForSeconds(3f);
        HideUI();

        _currentTrackingQuestID = null;
    }

    private void UpdateProgressText(int completedCount, int objectiveCount)
    {
        if (_progressText != null) _progressText.text = $"{completedCount} / {objectiveCount}";
    }
}
