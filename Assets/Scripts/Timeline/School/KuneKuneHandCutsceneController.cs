using System.Collections;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(DialogTimelineController))]
public class KuneKuneHandCutsceneController : CutsceneControllerBase
{
    private const int CutsceneCameraPriority = 20;
    // LookAround 클립의 최대 X 이동량을 실제 화면의 좌우 경계에 맞춰 환산합니다.
    private const float CameraPanClipDistance = 26f;

    [Header("KuneKune Hand")]
    [SerializeField] private KuneKuneHand _event;
    private Light2D _globalLight;
    private CinemachineCamera _cutsceneCamera;
    private Transform _cameraPanRoot;
    private PrioritySettings _cameraPriorityBeforeCutscene;
    private bool _hasCameraPriorityOverride;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {   
            // 여자 화장실도 손 컷씬을 기다리기 전에 실제 추격 적부터 없앱니다.
            MapManager.Instance?.KuneKune?.Despawn();
            // 저장된 완료 상태는 손 이벤트의 LoadState가 호출되기 전에도 확인합니다.
            if (KuneKuneHand.IsCleared || (SaveManager.Instance != null &&
                (SaveManager.Instance.IsLoading || SaveManager.Instance.GetFlag(KuneKuneHand.ClearedFlag))))
                return;

            Init();
            StartCoroutine(WaitAndPlayCutscene());
        }
    }

    private void Init()
    {
        _globalLight = FindObjectsByType<Light2D>(FindObjectsSortMode.None)
        .FirstOrDefault(light => light.lightType == Light2D.LightType.Global);

        _globalLight.color = KuneKuneHand.EventLightColor;
    }
    
    protected override void Awake()
    {
        base.Awake();
        _cutsceneCamera = GetComponentInChildren<CinemachineCamera>(true);
        if (_cutsceneCamera != null)
        {
            // Timeline이 기록하는 로컬 좌표는 유지하고, 이동 범위만 별도 부모에서 조절합니다.
            _cameraPanRoot = new GameObject("Camera Pan Range").transform;
            _cameraPanRoot.SetParent(_cutsceneCamera.transform.parent, false);
            _cutsceneCamera.transform.SetParent(_cameraPanRoot, false);
        }
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
        ConfigureCameraPanRange();
        _cameraPriorityBeforeCutscene = _cutsceneCamera.Priority;
        _hasCameraPriorityOverride = true;
        _cutsceneCamera.PreviousStateIsValid = false;
        _cutsceneCamera.Priority = CutsceneCameraPriority;
    }

    private void ConfigureCameraPanRange()
    {
        CinemachineConfiner2D confiner = _cutsceneCamera.GetComponent<CinemachineConfiner2D>();
        Camera outputCamera = Camera.main;
        if (_cameraPanRoot == null || confiner == null || confiner.BoundingShape2D == null ||
            outputCamera == null)
            return;

        Bounds roomBounds = confiner.BoundingShape2D.bounds;
        float halfWidth = _cutsceneCamera.Lens.OrthographicSize * outputCamera.aspect;
        float leftX = Mathf.Min(roomBounds.center.x, roomBounds.min.x + halfWidth);
        float rightX = Mathf.Max(roomBounds.center.x, roomBounds.max.x - halfWidth);

        _cameraPanRoot.localPosition = Vector3.zero;
        Vector3 start = _cameraPanRoot.position;
        start.x = Mathf.Clamp(start.x, leftX, rightX);
        _cameraPanRoot.position = start;

        Vector3 end = start;
        end.x = rightX;
        float localDistance = _cameraPanRoot.parent.InverseTransformPoint(end).x -
            _cameraPanRoot.localPosition.x;
        // 경계를 넘는 목표를 조기에 제한하는 대신, 클립의 2초 키가 정확히 끝에 닿게 합니다.
        _cameraPanRoot.localScale = new Vector3(Mathf.Max(0f, localDistance) / CameraPanClipDistance, 1f, 1f);
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
