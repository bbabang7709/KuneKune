using UnityEngine;

[RequireComponent(typeof(QuestObjectiveReporter))]
public class CleaningSpot : QuestObjectiveReporter, IInteractable
{
    private bool _isCleaned;

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        QuestManager.Instance.StartQuest(QuestData.QuestID);
    }

    public void OnInteractPressed()
    {
        if (_isCleaned)
            return;

        _isCleaned = true;

        // 청소 이미지나 애니메이션을 적용합니다.

        TryReportCompleted();
    }
}