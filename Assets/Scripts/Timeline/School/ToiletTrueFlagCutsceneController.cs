using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(DialogTimelineController))]
public class ToiletTrueFlagCutsceneController : CutsceneControllerBase
{
    private const string TrueFlagFailed = "HasTrueFlagFailed";

    private const int CutsceneCameraPriority = 20;
    [SerializeField] private SunlightAreaRoot _sunlightAreaRoot;
    private MapArea _sunlightMap;

    [SerializeField] private Transform _kuneKune;
    private Animator _kuneKuneAnimator;

    private AudioSource _handScratchSource;

    private float _kuneKuneAnimationSpeedBeforePause;
    private bool _isKuneKuneAnimationPaused;
    private bool _resumeKuneKuneAnimationAfterDialog;

    [Tooltip("이 대사가 진행되는 동안 컷씬용 쿠네쿠네의 이동을 멈춥니다.")]
    [SerializeField] private DialogData _stopKuneKuneDialog;
    private DialogTimelineController _dialogController;

    [SerializeField] private SpriteRenderer _blood;
    [SerializeField] private SpriteRenderer _water;

    [Header("화면 암전")]
    [Tooltip("시그널 호출 후 화면이 완전히 어두워지는 데 걸리는 시간(초).")]
    [SerializeField, Min(0f)] private float fadeOutDuration = 2f;

    [Header("컷씬 카메라")]
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

    private bool _trueFlagFailedWithoutSaveManager;
    private bool _trueFlagSucceededWithoutSaveManager;

    public bool _isMovingKuneKune;
    private bool _isKuneKunePausedForDialog;
    private bool _isPlayRequested;
    private bool _canInteractWithKuneKune;

    public bool CanInteractWithKuneKune =>
        _canInteractWithKuneKune && !HasTrueFlagFailed && !HasTrueFlagSucceeded && _kuneKune != null &&
        _kuneKune.gameObject.activeInHierarchy && _sunlightMap != null &&
        MapManager.Instance != null && MapManager.Instance.CurrentMap == _sunlightMap &&
        (SaveManager.Instance == null || !SaveManager.Instance.IsLoading);

    // 로드로 SaveData가 교체되어도 이전 실행의 실패 여부를 캐시하지 않는다.
    public bool HasTrueFlagFailed =>
        SaveManager.Instance?.GetFlag(TrueFlagFailed) ?? _trueFlagFailedWithoutSaveManager;

    public bool HasTrueFlagSucceeded =>
        SaveManager.Instance?.IsTrueEndingUnlocked ?? _trueFlagSucceededWithoutSaveManager;

    public void CompleteTrueFlagInteraction()
    {
        if (!CanInteractWithKuneKune) return;

        _canInteractWithKuneKune = false;
        _trueFlagSucceededWithoutSaveManager = true;
        SaveManager.Instance?.UnlockTrueEnding();
    }

    protected override void Awake()
    {
        base.Awake();
        _dialogController = GetComponent<DialogTimelineController>();
        if (_kuneKune != null)
            _kuneKuneAnimator = _kuneKune.GetComponent<Animator>();

        if (_blood == null)
            _blood = GetComponentInChildren<SpriteRenderer>(true);

        if (_sunlightAreaRoot == null)
            _sunlightAreaRoot = GetComponentInChildren<SunlightAreaRoot>(true);

        _sunlightAreaRoot.Deactivate();

        if (_cutsceneCamera == null)
            _cutsceneCamera = GetComponentInChildren<CinemachineCamera>(true);

        if (_cutsceneCamera == null)
            _cutsceneCamera = GetComponentInChildren<CinemachineCamera>(true);

        if (_bound == null && _cutsceneCamera != null)
            _bound = _cutsceneCamera.GetComponent<CinemachineConfiner2D>();

        _kuneKune.gameObject.SetActive(false);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _dialogController.DialogStarted += OnDialogStarted;
        _dialogController.DialogFinished += OnDialogFinished;
    }

    private void OnDialogStarted(DialogData dialog)
    {
        if (_stopKuneKuneDialog == null || dialog != _stopKuneKuneDialog) return;

        _isKuneKunePausedForDialog = true;
        StopKuneKune();
        _resumeKuneKuneAnimationAfterDialog = !_isKuneKuneAnimationPaused;
        PauseKuneKuneAnimation();
    }

    private void OnDialogFinished(DialogData dialog)
    {
        if (!_isKuneKunePausedForDialog || dialog != _stopKuneKuneDialog) return;

        _isKuneKunePausedForDialog = false;
        _isMovingKuneKune = _kuneKune != null && _kuneKune.gameObject.activeInHierarchy;
        if (_resumeKuneKuneAnimationAfterDialog)
            ResumeKuneKuneAnimation();
        _resumeKuneKuneAnimationAfterDialog = false;
    }

    public void Play()
    {
        if (_isPlayRequested || IsCutsceneRunning || HasTrueFlagFailed || HasTrueFlagSucceeded ||
            (playOnlyOnce && hasPlayed))
            return;

        _isPlayRequested = true;
        _canInteractWithKuneKune = false;

        if (_water != null)
            _water.sprite = null;

        // 대기 코루틴이 PlayCutscene을 호출할 때까지의 프레임 틈에 조작이 새지 않게 먼저 잠급니다.
        HoldControls();
        StartCoroutine(WaitAndPlayCutscene());
    }

    private IEnumerator WaitAndPlayCutscene()
    {
        if (SaveManager.Instance != null)
        {
            yield return new WaitUntil(() => SaveManager.Instance.IsLoading == false);
            // 로드가 끝나며 잠금이 모두 지워질 수 있으므로 다시 잠급니다.
            HoldControls();
        }

        yield return new WaitUntil(() =>
            (SaveManager.Instance == null || !SaveManager.Instance.IsLoading) &&
            (CutsceneManager.Instance == null || !CutsceneManager.Instance.IsCutscenePlaying) &&
            (DialogSystem.Instance == null || !DialogSystem.Instance.IsDialogPlaying));

        // 직접 Play가 호출되어도 로드된 실패 기록을 확인한 뒤에만 재생한다.
        if (HasTrueFlagFailed || HasTrueFlagSucceeded || (playOnlyOnce && hasPlayed))
        {
            _isPlayRequested = false;
            ReleaseControls();
            yield break;
        }

        if (_water != null)
            _water.sprite = null;

        _isPlayRequested = false;
        PlayCutscene();
    }

    protected override void OnCutsceneEndSetup()
    {
        RestoreDialogFadeOrder();
        RestoreCameraPriority();

        _sunlightMap = MapManager.Instance?.CurrentMap;
        _sunlightAreaRoot.gameObject.SetActive(true);
    }

    protected override void OnCutsceneFinished()
    {
        _canInteractWithKuneKune = true;
        if (_handScratchSource == null)
            _handScratchSource = SoundManager.Instance.CreateSFXSource(transform);

        SoundManager.Instance.PlaySFXOnSource(_handScratchSource, "KuneKuneHand_Scratch", loop: true);
    }

    private void FixedUpdate()
    {
        if (_isKuneKunePausedForDialog) return;

        if (_isMovingKuneKune && _kuneKune != null)
        {
            float moveSpeed = 1f;
            _kuneKune.position += Vector3.left * moveSpeed * Time.fixedDeltaTime;
            
            // 목표 위치에 도달했는지 확인
            if (Vector3.Distance(_kuneKune.position, MapManager.Instance.Player.transform.position) < 0.1f)
                _isMovingKuneKune = false; // 이동 완료
        }
    }

    private void LateUpdate()
    {
        // 같은 화장실 안에서 영역을 벗어나는 것과 문을 통해 맵을 나가는 것을 구분한다.
        if (_sunlightMap == null || MapManager.Instance?.CurrentMap == null)
            return;

        if (MapManager.Instance.CurrentMap == _sunlightMap)
            return;

        _blood.sprite = null;
        _canInteractWithKuneKune = false;

        _sunlightAreaRoot?.Deactivate();
        _sunlightMap = null;

        StopKuneKune();
        _kuneKune?.gameObject.SetActive(false);

        // 손수건으로 안 닦아주고 그냥 나감
        if (!HasTrueFlagSucceeded)
        {
            _trueFlagFailedWithoutSaveManager = true;
            SaveManager.Instance?.SetFlag(TrueFlagFailed, true);
        }

        PlayerMovement p = MapManager.Instance?.Player.GetComponent<PlayerMovement>();
        if (p.CurrentHeat >= p.MaxHeat)
            p.SetCurrentHeat(p.MaxHeat * 0.95f);
    }

    protected override void OnDisable()
    {
        if (_dialogController != null)
        {
            _dialogController.DialogStarted -= OnDialogStarted;
            _dialogController.DialogFinished -= OnDialogFinished;
        }
        _isKuneKunePausedForDialog = false;
        _isPlayRequested = false;
        _canInteractWithKuneKune = false;
        StopKuneKune();
        ResumeKuneKuneAnimation();
        _resumeKuneKuneAnimationAfterDialog = false;
        base.OnDisable();
        _sunlightAreaRoot?.Deactivate();
        _sunlightMap = null;
        RestoreDialogFadeOrder();
        ReleaseCutsceneCamera();
    }

    private void RestoreDialogFadeOrder()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.RestoreDialogFadeOrder();
    }

    /// <summary>
    /// 타임라인 시그널에서 호출하여 화면을 서서히 암전시킵니다.
    /// </summary>
    public void FadeOut()
    {
        if (UIManager.Instance == null)
            return;

        StartCoroutine(UIManager.Instance.FadeOutKeepingDialog(Mathf.Max(0f, fadeOutDuration)));
    }

    public void FadeIn()
    {
        if (UIManager.Instance == null)
            return;
        StartCoroutine(UIManager.Instance.FadeInKeepingDialog(Mathf.Max(0f, fadeOutDuration)));
    }

    public void SpawnKuneKune()
    {
        if (_kuneKune == null)
        {
            Debug.LogWarning("쿠네쿠네 오브젝트를 지정해주세요.", this);
            return;
        }

        _kuneKune.gameObject.SetActive(true);
        _isMovingKuneKune = !_isKuneKunePausedForDialog;
        if (!_isKuneKunePausedForDialog)
            ResumeKuneKuneAnimation();
    }

    public void StopKuneKune()
    {
        _isMovingKuneKune = false;
        if (_handScratchSource != null)
            _handScratchSource.Stop();
    }

    /// <summary>
    /// 컷씬용 쿠네쿠네의 애니메이션을 현재 프레임에서 일시 정지합니다.
    /// </summary>
    public void PauseKuneKuneAnimation()
    {
        if (_kuneKuneAnimator == null || _isKuneKuneAnimationPaused) return;

        _kuneKuneAnimationSpeedBeforePause = _kuneKuneAnimator.speed;
        _kuneKuneAnimator.speed = 0f;
        _isKuneKuneAnimationPaused = true;
    }

    /// <summary>
    /// 일시 정지한 애니메이션을 기존 속도로 이어서 재생합니다.
    /// </summary>
    public void ResumeKuneKuneAnimation()
    {
        if (!_isKuneKuneAnimationPaused) return;

        if (_kuneKuneAnimator != null)
            _kuneKuneAnimator.speed = _kuneKuneAnimationSpeedBeforePause;
        _isKuneKuneAnimationPaused = false;
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

}
