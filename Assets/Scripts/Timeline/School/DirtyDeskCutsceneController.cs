using UnityEngine;

public class DirtyDeskCutsceneController : CutsceneControllerBase
{
    [SerializeField] private FootprintSequence footprintSequence;
    [SerializeField] private GameObject rewardItem;
    private PlayerLight playerLight;

    private void Start()
    {
        if (rewardItem != null) rewardItem.SetActive(false);
        playerLight = FindFirstObjectByType<PlayerLight>();
    }

    public void Flicker()
    {
        if (LightingEventManager.Instance == null) return;
        
        StartCoroutine(LightingEventManager.Instance.FlickerRoutine());
    }

    public void ResetScene()
    {
        if (LightingEventManager.Instance != null)
        {
            LightingEventManager.Instance.SetGlobalLightColor(Color.white);
            LightingEventManager.Instance.SetBlackout(1f, 0.5f);
        }
        if (footprintSequence != null) footprintSequence.HideFootprint();
        if (playerLight != null) playerLight.TurnOff();
        if (rewardItem != null) rewardItem.SetActive(true);
    }
}
