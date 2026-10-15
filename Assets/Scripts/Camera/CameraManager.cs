using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private const int ActiveCameraPriority = 10;
    private const int InactiveCameraPriority = 0;

    public static CameraManager Instance { get; private set; }

    private CinemachineCamera currentCamera;
    public CinemachineCamera CurrentCamera => currentCamera;
    private Transform followTarget;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ResolveFollowTarget();
    }

    /// <summary>
    /// 플레이어가 Awake 시점에 없었거나 교체된 경우를 대비해 따라갈 대상을 다시 찾는다.
    /// </summary>
    private Transform ResolveFollowTarget()
    {
        if (followTarget == null)
            followTarget = GameObject.FindGameObjectWithTag("Player")?.transform;

        return followTarget;
    }

    public void ChangeCameraWithFollow(CinemachineCamera targetCamera)
    {
        if (targetCamera == null || currentCamera == targetCamera)
            return;

        Transform target = ResolveFollowTarget();
        if (target != null) targetCamera.Follow = target;

        SwitchTo(targetCamera);
    }

    public void ChangeCamera(CinemachineCamera targetCamera)
    {
        if (targetCamera == null || currentCamera == targetCamera)
            return;

        SwitchTo(targetCamera);
    }

    private void SwitchTo(CinemachineCamera targetCamera)
    {
        // 이전 카메라의 Follow는 유지한다. 블렌드 도중 Follow를 끊으면 나가는 카메라가 멈춰 화면이 튄다.
        if (currentCamera != null)
            currentCamera.Priority = InactiveCameraPriority;

        targetCamera.PreviousStateIsValid = false;
        targetCamera.Priority = ActiveCameraPriority;

        currentCamera = targetCamera;
    }
}
