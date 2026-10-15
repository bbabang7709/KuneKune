using System.Collections;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(DialogTimelineController))]
public class KuneKuneHandCutsceneController : CutsceneControllerBase
{
    private const int CutsceneCameraPriority = 20;

    [Header("KuneKune Hand")]
    [SerializeField] private KuneKuneHand _event;
    private Light2D _globalLight;
    private CinemachineCamera _cutsceneCamera;
    private PrioritySettings _cameraPriorityBeforeCutscene;
    private bool _hasCameraPriorityOverride;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {   
            // 여자 화장실도 손 컷씬을 기다리기 전에 실제 추격 적부터 없앱니다.
            MapManager.Instance?.KuneKune?.Despawn();
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
        _cutsceneCamera = GetComponentInChildren<CinemachineCamera>(true);
        if (_event == null)
            _event = FindFirstObjectByType<KuneKuneHand>();
    }

    protected override void OnCutsceneStartSetup()
    {
        if (_cutsceneCamera == null)
        {
            Debug.LogError("손 컷씬의 자식 오브젝트에 CinemachineCamera가 필요합니다.", this);
            return;
        }

        // 우선순위는 애니메이션 클립 대신 컷씬 전체 재생 구간에서 관리합니다.
        _cameraPriorityBeforeCutscene = _cutsceneCamera.Priority;
        _hasCameraPriorityOverride = true;
        _cutsceneCamera.PreviousStateIsValid = false;
        _cutsceneCamera.Priority = CutsceneCameraPriority;
    }

    protected override void OnCutsceneEndSetup()
    {
        // 종료 페이드가 끝난 검은 화면에서 화장실 카메라로 돌아갑니다.
        RestoreCameraPriority();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        RestoreCameraPriority();
    }

    private void RestoreCameraPriority()
    {
        if (!_hasCameraPriorityOverride)
            return;

        if (_cutsceneCamera != null)
            _cutsceneCamera.Priority = _cameraPriorityBeforeCutscene;

        _hasCameraPriorityOverride = false;
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
