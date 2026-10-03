using UnityEngine;

public class DirtyDeskCutsceneController : CutsceneControllerBase
{
    [SerializeField] private FootprintSequence footprintSequence;
    [SerializeField] private GameObject rewardItem;

    private void Start()
    {
        if (rewardItem != null) rewardItem.SetActive(false);
    }

    public void Flicker()
    {
        if (LightingEventManager.Instance == null) return;
        
        StartCoroutine(LightingEventManager.Instance.FlickerRoutine());
    }

    public void ResetScene()
    {
        if (LightingEventManager.Instance != null) LightingEventManager.Instance.SetBlackout(1f, 0.5f);
        if (footprintSequence != null) footprintSequence.HideFootprint();
        if (rewardItem != null) rewardItem.SetActive(true);
    }
}
