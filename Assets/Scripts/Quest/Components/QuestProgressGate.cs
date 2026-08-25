using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 지정한 퀘스트 대상이 모두 끝날 때까지
/// 같은 오브젝트의 상호작용 기능을 꺼 둡니다.
/// </summary>
[DisallowMultipleComponent]
public class QuestProgressGate : MonoBehaviour
{
    [Header("잠금 해제 조건")]
    [Tooltip("진행 상태를 확인할 퀘스트 데이터입니다.")]
    [SerializeField] private QuestData _questData;

    [Tooltip("이 대상들이 모두 끝나면 상호작용할 수 있습니다.")]
    [SerializeField] private string[] _requiredObjectiveIDs;

    [Header("잠겨 있을 때 표시")]
    [Tooltip("잠겨 있는 동안 이 오브젝트와 자식의 그림 및 애니메이션을 숨깁니다.")]
    [SerializeField] private bool _hideVisualsWhileLocked;

    private readonly List<MonoBehaviour> _interactableComponents = new();

    private Renderer[] _renderers;
    private Animator[] _animators;

    private bool _isUnlocked;

    /// <summary>
    /// 현재 상호작용 잠금이 풀렸는지 알려줍니다.
    /// </summary>
    public bool IsUnlocked => _isUnlocked;

    private void Awake()
    {
        FindInteractableComponents();
        SetInteractableComponentsEnabled(false);

        if (_hideVisualsWhileLocked)
        {
            FindVisualComponents();
            SetVisualComponentsEnabled(false);
        }
    }

    private void Start()
    {
        if (CanCheckProgress() == false)
            return;

        QuestManager.Instance.OnQuestProgressChanged += HandleQuestProgressChanged;

        // 이 오브젝트가 늦게 켜진 경우에도 이전 완료 상태를 반영합니다.
        RefreshUnlockedState();
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestProgressChanged -= HandleQuestProgressChanged;
    }

    /// <summary>
    /// 같은 오브젝트에서 플레이어가 상호작용할 수 있는 기능을 찾습니다.
    /// </summary>
    private void FindInteractableComponents()
    {
        MonoBehaviour[] components = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            if (component != null && component is IInteractable)
                _interactableComponents.Add(component);
        }

        if (_interactableComponents.Count == 0)
            Debug.LogWarning($"[{nameof(QuestProgressGate)}] 같은 오브젝트에 IInteractable을 구현한 컴포넌트가 없습니다.", this);
    }

    /// <summary>
    /// 열쇠 그림과 깜빡임 애니메이션을 담당하는 컴포넌트를 찾습니다.
    /// 꺼져 있는 자식 오브젝트도 검색합니다.
    /// </summary>
    private void FindVisualComponents()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _animators = GetComponentsInChildren<Animator>(true);
    }

    private void SetInteractableComponentsEnabled(bool isEnabled)
    {
        foreach (MonoBehaviour component in _interactableComponents)
        {
            if (component != null)
                component.enabled = isEnabled;
        }
    }

    private void SetVisualComponentsEnabled(bool isEnabled)
    {
        foreach (Renderer targetRenderer in _renderers)
        {
            if (targetRenderer != null)
                targetRenderer.enabled = isEnabled;
        }

        foreach (Animator targetAnimator in _animators)
        {
            if (targetAnimator != null)
                targetAnimator.enabled = isEnabled;
        }
    }

    /// <summary>
    /// 퀘스트 진행 상태를 확인할 준비가 되었는지 검사합니다.
    /// </summary>
    private bool CanCheckProgress()
    {
        if (_questData == null)
        {
            Debug.LogWarning($"[{nameof(QuestProgressGate)}] 퀘스트 데이터가 설정되지 않았습니다.", this);
            return false;
        }

        if (_requiredObjectiveIDs == null || _requiredObjectiveIDs.Length == 0)
        {
            Debug.LogWarning($"[{nameof(QuestProgressGate)}] 잠금 해제에 필요한 대상 ID가 없습니다.", this);
            return false;
        }

        if (QuestManager.Instance == null)
        {
            Debug.LogWarning($"[{nameof(QuestProgressGate)}] QuestManager를 찾을 수 없습니다.", this);
            return false;
        }

        return true;
    }

    private void HandleQuestProgressChanged(string questID, int completedCount, int objectiveCount)
    {
        if (questID != _questData.QuestID)
            return;

        RefreshUnlockedState();
    }

    /// <summary>
    /// 필요한 대상이 모두 끝났다면 상호작용 잠금을 풉니다.
    /// </summary>
    private void RefreshUnlockedState()
    {
        if (_isUnlocked)
            return;

        bool requirementsCompleted = QuestManager.Instance.AreObjectivesCompleted(_questData.QuestID, _requiredObjectiveIDs);
        if (requirementsCompleted == false)
            return;

        _isUnlocked = true;
        SetInteractableComponentsEnabled(true);

        if (_hideVisualsWhileLocked)
            SetVisualComponentsEnabled(true);

        Debug.Log($"[Quest] 상호작용 잠금 해제: {gameObject.name}", this);
    }
}
