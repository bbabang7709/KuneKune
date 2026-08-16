using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    private CinemachineCamera currentCamera;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetCamera(CinemachineCamera targetCamera)
    {
        if (targetCamera == null || (currentCamera == targetCamera)) return;

        if (currentCamera != null) currentCamera.Priority = 0;
        targetCamera.Priority = 10;
        currentCamera = targetCamera;
    }
}
