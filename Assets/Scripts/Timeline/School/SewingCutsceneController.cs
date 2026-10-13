using Unity.Cinemachine;
using UnityEngine;

public class SewingCutsceneController : CutsceneControllerBase
{
    [SerializeField] private CinemachineCamera cm;
    [SerializeField] private Door door;
    protected override void OnCutsceneEndSetup()
    {
        if (cm != null) cm.Priority = 0;
        door.SetLockState(false);
    }
}
