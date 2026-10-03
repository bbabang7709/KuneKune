using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(DialogTimelineController))]
public class KuneKuneHandCutsceneController : CutsceneControllerBase
{
    [Header("KuneKune Hand")]
    [SerializeField] private KuneKuneHand _event;
    private Light2D _globalLight;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {   
            Init();
            StartCoroutine(WaitAndPlayCutscene());
        }
    }

    private void Init()
    {
        _globalLight = FindObjectsByType<Light2D>(FindObjectsSortMode.None)
        .FirstOrDefault(light => light.lightType == Light2D.LightType.Global);

        _globalLight.color = new Color32(19, 12, 12, 255);
    }
    
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

    private IEnumerator WaitAndPlayCutscene()
    {
        if (ControlLockManager.Instance != null)
        {
            yield return new WaitUntil(() => 
                !ControlLockManager.Instance.IsMovementLocked 
                && !ControlLockManager.Instance.IsInputLocked
            );
        }

        PlayCutscene();
    }
}
