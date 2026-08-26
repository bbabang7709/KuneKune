using UnityEngine;
using Unity.Cinemachine;
using Unity.VisualScripting;

/// <summary>
/// 플레이어가 상호작용하여 다른 위치로 이동할 수 있는 문입니다.
/// 
/// 문이 잠겨 있는 경우 필요한 열쇠를 확인하고,
/// 열쇠가 없다면 지정된 대사를 출력합니다.
/// 
/// 문이 정상적으로 열리면 플레이어를 지정된 Spawn Point로 이동시키고
/// 연결된 Cinemachine Camera로 카메라를 전환합니다.
/// </summary>
public class Door : MonoBehaviour, IInteractable
{
    [Header("Link Settings")]
    [SerializeField] private EntryPoint targetEntryPoint;
    [SerializeField] private MapArea targetMap;

    [Header("Lock Settings")]
    [SerializeField] private bool isLocked;

    [SerializeField] private string requiredKeyID;

    [SerializeField] private DialogData dialogWhenLocked;

    [Header("추격 잠금 설정")]
    [Tooltip("집 안 추격이 시작되면 이 문을 열 수 없게 합니다.")]
    [SerializeField] private bool _lockDuringHouseChase;

    private bool _isLockedByHouseChase;

    /// <summary>
    /// 집 안 추격 중 잠가야 하는 문인지 알려줍니다.
    /// </summary>
    public bool LockDuringHouseChase => _lockDuringHouseChase;

    /// <summary>
    /// 상호작용 키를 눌렀을 때 호출됩니다.
    /// 
    /// 문이 잠겨 있다면 필요한 열쇠를 확인하고,
    /// 열쇠가 없을 경우 잠긴 상태의 대사를 출력합니다.
    /// 
    /// 문을 열 수 있는 경우 플레이어를 Target Spawn Point로 이동시키고
    /// 연결된 카메라로 전환합니다.
    /// </summary>
    public void OnInteractPressed()
    {
        // 추격 잠금은 열쇠 보유 여부와 상관없이 문을 열 수 없습니다.
        if (_isLockedByHouseChase)
        {
            Debug.Log("[Door] 추격 중에는 이 문을 열 수 없습니다.", this);
            PlayLockedDialog();
            return;
        }

        if (targetEntryPoint == null)
        {
            Debug.LogWarning(
                $"[{nameof(Door)}] Target Entry Point가 설정되지 않았습니다.",
                this
            );

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

        MapManager.Instance?.ChangeMap(targetMap, targetEntryPoint);
        // MovePlayer();
        // ChangeCamera();
    }

    /// <summary>
    /// 집 안 추격이 시작되거나 끝날 때 문 상태를 바꿉니다.
    /// 기존 열쇠 잠금 상태는 변경하지 않습니다.
    /// </summary>
    public void SetHouseChaseLocked(bool isLocked)
    {
        _isLockedByHouseChase = isLocked;
    }

    /// <summary>
    /// 현재 플레이어의 인벤토리를 확인하여
    /// 문을 열 수 있는지 검사합니다.
    /// </summary>
    private bool CheckCanOpen()
    {
        // requiredKeyID가 비어 있다면
        // 별도의 열쇠가 필요하지 않는 것으로 처리합니다.
        if (string.IsNullOrEmpty(requiredKeyID))
        {
            return true;
        }

        return InventoryManager.Instance != null &&
               InventoryManager.Instance.HasKey(requiredKeyID);
    }

    /// <summary>
    /// 문이 잠겨 있을 때 지정된 대사를 재생합니다.
    /// </summary>
    private void PlayLockedDialog()
    {
        if (dialogWhenLocked == null)
            return;

        DialogSystem.Instance?.PlayDialog(dialogWhenLocked);
    }
}
