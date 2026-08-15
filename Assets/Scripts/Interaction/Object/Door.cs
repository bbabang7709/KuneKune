using Unity.Cinemachine;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    [Header("Link Settings")]
    [SerializeField] private Transform targetSpawnPoint;
    [SerializeField] private CinemachineCamera targetCamera;
    private GameObject user;

    public void OnInteractPressed()
    {
        if (user == null) return;
        if (targetSpawnPoint == null) return;

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
        CameraManager.Instance.SetCamera(targetCamera);
    }
}
