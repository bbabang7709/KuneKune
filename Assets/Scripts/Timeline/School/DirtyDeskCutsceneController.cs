using UnityEngine;

public class DirtyDeskCutsceneController : CutsceneControllerBase
{
    public void Flicker()
    {
        if (LightingEventManager.Instance == null) return;
        
        StartCoroutine(LightingEventManager.Instance.FlickerRoutine());
    }
}
