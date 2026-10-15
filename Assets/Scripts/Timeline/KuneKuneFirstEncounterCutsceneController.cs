using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class KuneKuneFirstEncounterCutsceneController : CutsceneControllerBase
{
    private const int CutsceneCameraPriority = 20;

    [SerializeField] private KuneKune _kuneKune;

    [Header("첫 조우 컷씬 카메라")]
    [SerializeField] private CinemachineCamera _cutsceneCamera;
    [SerializeField] private CinemachineConfiner2D _bound;

    private PrioritySettings _cameraPriorityBeforeCutscene;
    /// <summary>
    /// 컷씬 카메라가 우선순위 설정되었는지 확인
    /// </summary>
    private bool _hasCameraPriorityOverride;
    /// <summary>
    /// 컷씬용 부드러운 전환 설정되었는지 확인
    /// </summary>
    private bool _isCameraBlendOverrideRegistered;
    private float _cameraBlendDuration = 2f;

    protected override void Awake()
    {
        base.Awake();
        // 첫 조우는 Spawn의 HasPlayed 검사와 PlayCutscene을 통해서만 시작합니다.
        director.playOnAwake = false;
        if (_kuneKune == null)
            _kuneKune = FindFirstObjectByType<KuneKune>();

        if (_cutsceneCamera == null && _kuneKune != null)
            _cutsceneCamera = _kuneKune.GetComponentInChildren<CinemachineCamera>(true);

        if (_bound == null && _cutsceneCamera != null)
            _bound = _cutsceneCamera.GetComponent<CinemachineConfiner2D>();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        ReleaseCutsceneCamera();
    }

    protected override void OnCutsceneEndSetup()
    {
        // 경계와 블렌드 설정을 유지한 채 플레이어 카메라로 복귀합니다.
        RestoreCameraPriority();
    }

    #region 카메라 세팅
    /// <summary>
    /// Timeline Signal에서 호출합니다. duration초 동안 현재 카메라에서 CM으로 이동합니다.
    /// </summary>
    public void BlendToCutsceneCamera(float duration)
    {
        if (_cutsceneCamera == null)
        {
            Debug.LogWarning("첫 조우 컷씬의 CM 카메라를 지정해주세요.", this);
            return;
        }

        _cameraBlendDuration = Mathf.Max(0f, duration);
        SetCameraBound();

        if (!_isCameraBlendOverrideRegistered)
        {
            CinemachineCore.GetBlendOverride += GetCameraBlend;
            _isCameraBlendOverrideRegistered = true;
        }

        if (!_hasCameraPriorityOverride)
        {
            _cameraPriorityBeforeCutscene = _cutsceneCamera.Priority;
            _hasCameraPriorityOverride = true;
        }

        _cutsceneCamera.gameObject.SetActive(true);
        _cutsceneCamera.enabled = true;
        _cutsceneCamera.PreviousStateIsValid = false;
        _cutsceneCamera.Priority = CutsceneCameraPriority;
    }

    private void SetCameraBound()
    {
        if (_bound == null)
            _bound = _cutsceneCamera.GetComponent<CinemachineConfiner2D>();

        if (_bound != null && _bound.BoundingShape2D != null)
            return;

        if (CameraManager.Instance == null || CameraManager.Instance.CurrentCamera == null)
            return;

        if (CameraManager.Instance.CurrentCamera == _cutsceneCamera)
            return;

        var playerConfiner = CameraManager.Instance.CurrentCamera.GetComponent<CinemachineConfiner2D>();
        if (playerConfiner == null || playerConfiner.BoundingShape2D == null)
            return;

        _bound.BoundingShape2D = playerConfiner.BoundingShape2D;
        _bound.InvalidateBoundingShapeCache();
    }

    private CinemachineBlendDefinition GetCameraBlend(
        ICinemachineCamera fromCamera, ICinemachineCamera toCamera,
        CinemachineBlendDefinition defaultBlend, UnityEngine.Object owner)
    {
        // 다른 맵 카메라의 기본 전환 설정은 유지합니다.
        if (_cutsceneCamera == null ||
            (!ReferenceEquals(fromCamera, _cutsceneCamera) && !ReferenceEquals(toCamera, _cutsceneCamera)))
            return defaultBlend;

        return new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, _cameraBlendDuration);
    }

    private void ReleaseCutsceneCamera()
    {
        if (_bound != null && _bound.BoundingShape2D != null)
            _bound.BoundingShape2D = null;

        // 종료 후에는 첫 조우 컷씬의 블렌드 시간이 다른 전환에 적용되지 않도록 해제합니다.
        if (_isCameraBlendOverrideRegistered)
        {
            CinemachineCore.GetBlendOverride -= GetCameraBlend;
            _isCameraBlendOverrideRegistered = false;
        }

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
    #endregion

    public void SpawnKuneKune()
    {
        if (_kuneKune == null)
            return;

        _kuneKune.gameObject.SetActive(true);
        _kuneKune.SpriteRenderer.sprite = null;
        _kuneKune.SpriteRenderer.enabled = true;
        if (_kuneKune.SpawnObject != null)
            _kuneKune.SpawnObject.SetActive(false);
        _kuneKune.State = ECharacterState.Spawning;
    }

    protected override void OnCutsceneFinished()
    {
        base.OnCutsceneFinished();
        _kuneKune.State = ECharacterState.Run;
    }

    public void Play()
    {
        if (HasPlayed || !isActiveAndEnabled) return;

        // 코루틴이 저장 로딩을 기다리는 동안에도 쿠네쿠네는 보이지 않아야 합니다.
        _kuneKune.SpriteRenderer.enabled = false;
        if (_kuneKune.SpawnObject != null)
            _kuneKune.SpawnObject.SetActive(true);
        _kuneKune.State = ECharacterState.Idle;
        StartCoroutine(WaitAndPlayCutscene());
    }

    private IEnumerator WaitAndPlayCutscene()
    {
        if (SaveManager.Instance != null)
        {
            yield return new WaitUntil(() => SaveManager.Instance.IsLoading == false);
        }

        PlayCutscene();
    }
}
