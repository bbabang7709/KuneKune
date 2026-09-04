using UnityEngine;

/// <summary>
/// 플레이어가 상호작용하여 다른 위치로 이동할 수 있는 문입니다.
/// </summary>
public class Door : MapTransition, IInteractable
{
    [Header("Lock Settings")]
    [SerializeField] private bool isLocked;
    [SerializeField] private string requiredKeyID;
    [SerializeField] private DialogData dialogWhenLocked;

    [Header("추격 잠금 설정")]
    [Tooltip("집 안 추격이 시작되면 이 문을 열 수 없게 합니다.")]
    [SerializeField] private bool _lockDuringHouseChase;

    private bool _isLockedByHouseChase;

    public bool LockDuringHouseChase => _lockDuringHouseChase;

    public void OnInteractPressed()
    {
        // 추격 잠금은 열쇠 보유 여부와 상관없이 문을 열 수 없습니다.
        if (_isLockedByHouseChase)
        {
            Debug.Log("[Door] 추격 중에는 이 문을 열 수 없습니다.", this);
            PlayLockedDialog();
            return;
        }

        // 부모 클래스(MapTransition)의 프로퍼티를 확인
        if (TargetEntryPoint == null || TargetMap == null)
        {
            Debug.LogWarning($"[{nameof(Door)}] Target Map 또는 Entry Point가 설정되지 않았습니다.", this);
            return;
        }

        if (isLocked && !CheckCanOpen())
        {
            PlayLockedDialog();
            return;
        }

        // 필요한 열쇠가 있다면 잠금 상태를 해제합니다.
        if (isLocked)
        {
            isLocked = false;
        }

        // 부모에 구현된 ID 기반 이동 메서드 호출 (페이드 연출 자동 수행)
        ExecuteTransition();
    }

    public void SetHouseChaseLocked(bool isLocked)
    {
        _isLockedByHouseChase = isLocked;
    }

    private bool CheckCanOpen()
    {
        if (string.IsNullOrEmpty(requiredKeyID))
        {
            return true;
        }

        return InventoryManager.Instance != null &&
               InventoryManager.Instance.HasKey(requiredKeyID);
    }

    private void PlayLockedDialog()
    {
        if (dialogWhenLocked == null) return;
        DialogSystem.Instance?.PlayDialog(dialogWhenLocked);
    }
}