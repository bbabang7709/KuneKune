using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    private CinemachineCamera currentCamera;
    private Transform followTarget;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        followTarget = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    public void ChangeCameraWithFollow(CinemachineCamera targetCamera)
    {
        if (targetCamera == null || currentCamera == targetCamera)
            return;

        if (currentCamera != null)
        {
            currentCamera.Priority = 0;
            currentCamera.Follow = null;
        }

        if (followTarget != null) targetCamera.Follow = followTarget;
        targetCamera.PreviousStateIsValid = false;
        targetCamera.Priority = 10;

        currentCamera = targetCamera;
    }

    public void ChangeCamera(CinemachineCamera targetCamera)
    {
        if (targetCamera == null || currentCamera == targetCamera)
            return;

        if (currentCamera != null)
        {
            currentCamera.Priority = 0;
            currentCamera.Follow = null;
        }

        targetCamera.PreviousStateIsValid = false;
        targetCamera.Priority = 10;

        currentCamera = targetCamera;
    }
}
