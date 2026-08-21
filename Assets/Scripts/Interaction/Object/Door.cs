using UnityEngine;
using Unity.Cinemachine;

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
    [SerializeField]
    private Transform targetSpawnPoint;

    [SerializeField]
    private CinemachineCamera targetCamera;

    [Header("Lock Settings")]
    [SerializeField]
    private bool isLocked;

    [SerializeField]
    private string requiredKeyID;

    [SerializeField]
    private DialogData dialogWhenLocked;

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
        if (targetSpawnPoint == null)
        {
            Debug.LogWarning(
                $"[{nameof(Door)}] Target Spawn Point가 설정되지 않았습니다.",
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

        MovePlayer();
        ChangeCamera();
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

    /// <summary>
    /// 플레이어를 문에 지정된 이동 위치로 이동시킵니다.
    /// </summary>
    private void MovePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning(
                $"[{nameof(Door)}] Player 태그를 가진 오브젝트를 찾을 수 없습니다.",
                this
            );

            return;
        }

        player.transform.position = targetSpawnPoint.position;
    }

    /// <summary>
    /// 문에 연결된 카메라로 전환합니다.
    /// </summary>
    private void ChangeCamera()
    {
        if (targetCamera == null)
            return;

        CameraManager.Instance?.SetCamera(targetCamera);
    }
}
