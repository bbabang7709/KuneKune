using UnityEngine;

[RequireComponent(typeof(DialogTimelineController))]
public class KuneKuneHandCutsceneController : CutsceneControllerBase
{
    [Header("KuneKune Hand")]
    [SerializeField] private KuneKuneHand _event;

    protected override void Awake()
    {
        base.Awake();
        if (_event == null)
            _event = FindFirstObjectByType<KuneKuneHand>();
    }

    //private void Start()
    //{
    //    //PlayCutscene();
    //}

    protected override void OnCutsceneFinished()
    {
        _event.Play();
    }
}
