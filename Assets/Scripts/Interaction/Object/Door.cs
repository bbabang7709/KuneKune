using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Door : MonoBehaviour, IInteractable
{
    [Header("Link Settings")]
    [SerializeField] private Transform targetSpawnPoint;
    [SerializeField] private CinemachineCamera targetCamera;

    [Header("Prompt Settings")]
    [SerializeField] private Transform promptPoint;
    public Transform PromptPoint => promptPoint;
    
    private GameObject user;

    [SerializeField] private bool isLocked = false;
    [SerializeField] private string requiredKeyID;

    public void OnInteractPressed()
    {
        if (user == null) return;
        if (targetSpawnPoint == null) return;

        if (isLocked)
        {
            if (InventoryManager.Instance.HasKey(requiredKeyID))
            {
                isLocked = false;
                Debug.Log($"{name}: 잠금 해제");
            }
            else
            {
                Debug.Log($"{name}: {requiredKeyID} 필요.");
                return;
            }
        }

        user.transform.position = targetSpawnPoint.position;
        ChangeCamera();
    }
    public void OnInteractHeld() {}
    public void OnInteractReleased() {}

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            user = collision.gameObject;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            user = null;
        }
    }

    private void ChangeCamera()
    {
        CameraManager.Instance?.SetCamera(targetCamera);
    }

    private bool CheckCanOpen()
    {
        if (InventoryManager.Instance.HasKey(requiredKeyID))
        {
            return true;
        }

        return false;   
    }
}
