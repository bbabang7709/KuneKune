using UnityEngine;

[RequireComponent(typeof(DialogTimelineController))]
public class KuneKuneHandCutsceneController : CutsceneControllerBase
{
    [Header("KuneKune Hand")]
    [SerializeField] private KuneKuneHand _event;

    private void Start()
    {
        PlayCutscene();
    }

    protected override void OnCutsceneFinished()
    {
        _event.Play();
    }
}
