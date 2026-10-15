using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using Unity.Cinemachine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(DialogTimelineController))]
public class TrueEnding : CutsceneControllerBase
{
    private const int CutsceneCameraPriority = 20;
    private const string TitleSceneName = "Scene_Intro";
    private const string EndingPianoBgmId = "TrueEnding_Piano";
    private static readonly string[] ApproachEchoPhrases = { "가지 마……", "외로워……" };

    [SerializeField] private AnimationClip playerSweatIdleClip;
    [SerializeField] private AnimationClip playerSweatIdleSideClip;
    [SerializeField] private AnimationClip playerWalkClip;
    [SerializeField] private AnimationClip playerHugClip;
    [SerializeField] private AnimationClip kuneKuneTrueEndingIdleClip;
    [SerializeField] private AnimationClip kuneKuneHugClip;
    [Tooltip("쿠네쿠네의 도착과 메아리 종료 후, 서로 바라보다 접근 전 대사를 시작하기까지의 시간입니다.")]
    [SerializeField, Min(0f)] private float playerHugDelay = 3f;
    [Tooltip("접근 전 마지막 대사가 끝난 뒤, 서로 바라보며 여운을 유지하는 시간입니다.")]
    [SerializeField, Min(0f)] private float playerApproachPause = 2f;
    [Tooltip("Cutscene_TrueEnding의 자식인 쿠네쿠네가 도착할 로컬 X좌표입니다.")]
    [SerializeField] private float kuneKuneArrivalLocalX = 115f;
    [SerializeField, Min(0.01f)] private float kuneKuneAdvanceSpeed = 1.5f;
    [SerializeField, Min(0.01f)] private float playerHugMoveSpeed = 1f;
    [Tooltip("기존 월드 X 33.3에서 확인한 포옹 간격을 쿠네쿠네의 위치에 맞춰 유지합니다.")]
    [SerializeField, Min(0f)] private float playerHugStopDistance = 3.7f;
    [SerializeField, Min(0f)] private float hugHoldDuration = 1f;
    [Tooltip("포옹 대사의 마지막 문장이 끝난 뒤, 흰 화면 전환 전까지 포옹을 유지하는 시간입니다.")]
    [SerializeField, Min(0f)] private float whiteFadeDelay = 2f;
    [Tooltip("짧은 장면에 맞춰 피아노 곡을 재생하기 시작할 위치(초)입니다.")]
    [SerializeField, Min(0f)] private float endingMusicStartTime = 21.5f;
    [SerializeField, Min(0.01f)] private float endingMusicFadeInDuration = 0.6f;
    [Tooltip("포옹 장면을 흰 화면으로 덮고 음악을 함께 줄이는 시간입니다.")]
    [SerializeField, Min(0.01f)] private float whiteFadeDuration = 2f;
    [SerializeField, Min(0.01f)] private float morningFadeDuration = 1f;
    [SerializeField, Min(0f)] private float morningDialogDelay = 2f;
    [SerializeField, Min(0f)] private float landscapeHoldDuration = 3f;
    [SerializeField, Min(0.01f)] private float endingFadeDuration = 1f;
    [SerializeField] private string endingTitle = "fin.";
    [SerializeField, Min(0.01f)] private float endingTitleFadeDuration = 1f;
    [SerializeField, Min(0f)] private float endingTitleHoldDuration = 3f;
    [SerializeField, Min(0f)] private float endingBlackHoldDuration = 2f;
    [SerializeField] private DialogData firstDialog;
    [SerializeField] private DialogData playerApproachDialog;
    [SerializeField] private DialogData playerHugDialog;
    [SerializeField] private DialogData playerEpilogueDialog;
    [SerializeField] private UnityEvent onTrueEndingFinished = new UnityEvent();
    [SerializeField] private Sprite morningSky;
    [SerializeField] private Sprite[] brightCloudSprites;

    [SerializeField] Transform kunekKune;
    [SerializeField] private CinemachineCamera trueEndingCamera;
#if UNITY_EDITOR
    [Header("에디터 O키 미리보기")]
    [SerializeField] private Transform editorPreviewStartPoint;
    private Vector3 _editorKuneKuneStartPosition;
    private bool _isEditorPreview;
#endif

    private PlayableGraph _playerAnimationGraph;
    private AnimationClipPlayable _playerAnimationPlayable;
    private PlayableGraph _kuneKuneAnimationGraph;
    private AnimationClipPlayable _kuneKuneAnimationPlayable;
    private bool _hugRequested;
    private Coroutine _delayedHugRoutine;
    private Coroutine _titleReturnRoutine;
    private bool _narrativeCompleted;
    private bool _endingBlackoutCompleted;
    private UIManager _transitionUI;
    private Color _fadeColorBeforeTransition;
    private bool _hasFadeColorOverride;
    private AudioSource _kuneKuneScratchSource;
    private SoundManager _bgmSoundManager;
    private bool _isEndingMusicPlaying;
    private DialogTimelineController _dialogController;
    private bool _isKuneKuneAdvancing;
    private bool _approachEchoFinished;
    private bool _kuneKuneAtApproachStop;
    private PlayerMovement _hugPlayerMovement;
    private PrioritySettings _cameraPriorityBeforeCutscene;
    private bool _cameraWasActive;
    private bool _cameraWasEnabled;
    private bool _hasCameraOverride;

    protected override void Awake()
    {
        base.Awake();
        _dialogController = GetComponent<DialogTimelineController>();
        if (trueEndingCamera == null)
            trueEndingCamera = GetComponentInChildren<CinemachineCamera>(true);
#if UNITY_EDITOR
        if (kunekKune != null)
            _editorKuneKuneStartPosition = kunekKune.position;
#endif
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _dialogController.DialogFinished += OnDialogFinished;
    }

    public override void PlayCutscene()
    {
        if (IsCutsceneRunning) return;
        base.PlayCutscene();
        if (IsCutsceneRunning)
            BlockBackgroundMusic();
    }

#if UNITY_EDITOR
    private void LateUpdate()
    {
        if (Keyboard.current == null || !Keyboard.current.oKey.wasPressedThisFrame ||
            IsCutsceneRunning || Time.timeScale <= 0f ||
            (SaveManager.Instance != null && SaveManager.Instance.IsLoading) ||
            (CutsceneManager.Instance != null && CutsceneManager.Instance.IsCutscenePlaying) ||
            (DialogSystem.Instance != null && DialogSystem.Instance.IsDialogPlaying) ||
            (ControlLockManager.Instance != null &&
                (ControlLockManager.Instance.IsInputLocked || ControlLockManager.Instance.IsMovementLocked)))
            return;

        if (MapManager.Instance?.Player == null || kunekKune == null ||
            editorPreviewStartPoint == null || director.playableAsset == null)
        {
            Debug.LogWarning("진엔딩 O키 미리보기의 플레이어, 쿠네쿠네, 시작 위치와 Timeline을 확인해주세요.", this);
            return;
        }

        // 선행 플래그와 1회 재생 제한 없이, 기존 컷씬 시작 절차로 진입합니다.
        _isEditorPreview = true;
        hasPlayed = false;
        HoldControls();
        MapManager.Instance.KuneKune?.Despawn();
        StopPlayerMovement();
        director.time = 0d;
        PlayCutscene();
    }

    private void PrepareEditorPreview()
    {
        _isEditorPreview = false;
        MapManager mapManager = MapManager.Instance;
        mapManager.InitMap(EMapType.Chase_Ricefield, "chase_start");
        kunekKune.position = _editorKuneKuneStartPosition;
        kunekKune.gameObject.SetActive(false);

        GameObject player = mapManager.Player;
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        movement?.StopImmediately();
        movement?.SetCurrentHeat(0f, true);
        player.GetComponent<PlayerAppearance>()?.SetAge(EPlayerAge.Child);
        Transform playerVisual = player.GetComponentInChildren<Animator>()?.transform;
        Vector3 visualOffset = playerVisual != null ? playerVisual.position - player.transform.position : Vector3.zero;
        Vector3 position = editorPreviewStartPoint.position;
        Collider2D startTrigger = editorPreviewStartPoint.GetComponent<Collider2D>();
        if (startTrigger != null)
            position.x = startTrigger.bounds.min.x;
        position.y = kunekKune.position.y - visualOffset.y;
        position.z = player.transform.position.z;
        player.transform.position = position;
        Physics2D.SyncTransforms();
    }
#endif

    private void BlockBackgroundMusic()
    {
        SoundManager soundManager = SoundManager.Instance;
        if (soundManager == null || _bgmSoundManager == soundManager)
            return;

        ReleaseBackgroundMusic();
        _bgmSoundManager = soundManager;
        _bgmSoundManager.SetBGMPlaybackBlocked(this, true);
    }

    private void ReleaseBackgroundMusic()
    {
        if (_bgmSoundManager != null)
            _bgmSoundManager.SetBGMPlaybackBlocked(this, false);
        _bgmSoundManager = null;
    }

    private void StopEndingMusic()
    {
        if (!_isEndingMusicPlaying) return;
        _isEndingMusicPlaying = false;
        SoundManager.Instance?.StopBGM(0f);
    }

    private void OnDialogFinished(DialogData dialog)
    {
        if (firstDialog != null && dialog == firstDialog)
            PlayPlayerSweatIdleSide();
    }

    /// <summary>
    /// 타임라인 시그널에서 실제 플레이어의 땀 흘리는 어린이 대기 애니메이션을 재생합니다.
    /// </summary>
    public void PlayPlayerSweatIdle()
    {
        if (_hugRequested) return;
        PlayPlayerAnimation(playerSweatIdleClip);
    }

    public void PlayPlayerSweatIdleSide()
    {
        if (!isActiveAndEnabled || _hugRequested) return;
        MapManager.Instance?.Player?.GetComponent<PlayerMovement>()?.SetFacingDirection(-1f);
        PlayPlayerAnimation(playerSweatIdleSideClip);
    }

    private bool PlayPlayerAnimation(AnimationClip clip, float speed = 1f)
    {
        if (!isActiveAndEnabled) return false;
        Animator animator = MapManager.Instance?.Player?.GetComponentInChildren<Animator>();
        if (animator == null || clip == null)
        {
            Debug.LogWarning("진엔딩의 실제 플레이어 Animator와 애니메이션 클립을 확인해주세요.", this);
            return false;
        }

        StopPlayerAnimation();
        _playerAnimationPlayable = AnimationPlayableUtilities.PlayClip(animator, clip, out _playerAnimationGraph);
        _playerAnimationPlayable.SetSpeed(speed);
        _playerAnimationGraph.Evaluate(0f);
        return true;
    }

    /// <summary>
    /// 타임라인 시그널에서 쿠네쿠네를 지정한 로컬 X까지 이동시키고 메아리가 끝나면 다음 연출로 이어갑니다.
    /// </summary>
    public void MoveKuneKuneForward()
    {
        if (!isActiveAndEnabled || _hugRequested)
            return;

        GameObject player = MapManager.Instance != null ? MapManager.Instance.Player : null;
        if (kunekKune == null || player == null)
        {
            Debug.LogWarning("진엔딩의 쿠네쿠네와 실제 플레이어를 확인해주세요.", this);
            return;
        }

        StopDelayedHug();
        StopPlayerMovement();
        StopKuneKuneAnimation();
        kunekKune.gameObject.SetActive(true);
        Animator animator = kunekKune.GetComponent<Animator>();
        if (animator != null)
            animator.Play("Cutscene_ToiletTrueFlag", 0, 0f);

        SpriteRenderer spriteRenderer = kunekKune.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.flipX = true;

        _isKuneKuneAdvancing = true;
        _approachEchoFinished = false;
        _kuneKuneAtApproachStop = false;
        PlayKuneKuneScratch();
        UIManager.Instance?.PlayTrueEndingEcho(ApproachEchoPhrases, OnApproachEchoCompleted);
    }

    private void PlayKuneKuneScratch()
    {
        SoundManager soundManager = SoundManager.Instance;
        if (soundManager == null || kunekKune == null)
            return;

        if (_kuneKuneScratchSource == null)
            _kuneKuneScratchSource = soundManager.CreateSFXSource(kunekKune);
        soundManager.PlaySFXOnSource(_kuneKuneScratchSource, "KuneKuneHand_Scratch", loop: true);
    }

    private void StopKuneKuneAdvance()
    {
        _isKuneKuneAdvancing = false;
        if (_kuneKuneScratchSource != null)
            _kuneKuneScratchSource.Stop();
    }

    private void OnApproachEchoCompleted()
    {
        _approachEchoFinished = true;
        if (_kuneKuneAtApproachStop)
            CompleteKuneKuneApproach();
    }

    private void CompleteKuneKuneApproach()
    {
        StopKuneKuneAdvance();
        if (!isActiveAndEnabled || _hugRequested)
            return;

        StopPlayerMovement();
        StopKuneKuneAnimation();
        Animator animator = kunekKune != null ? kunekKune.GetComponent<Animator>() : null;
        if (animator != null)
            animator.Play("Cutscene_ToiletTrueFlag_Idle", 0, 0f);

        SpriteRenderer spriteRenderer = kunekKune != null ? kunekKune.GetComponent<SpriteRenderer>() : null;
        if (spriteRenderer != null)
            spriteRenderer.flipX = true;

        MapManager.Instance?.Player?.GetComponent<PlayerMovement>()?.StopImmediately();
        // 도착과 메아리가 끝나면 서로 3초 바라보고, 종료 루틴에서 대사를 마친 뒤 다가갑니다.
        PlayPlayerSweatIdleSide();
        StopDelayedHug();
        _delayedHugRoutine = StartCoroutine(RequestHugAfterDelay());
    }

    private IEnumerator RequestHugAfterDelay()
    {
        yield return new WaitForSeconds(playerHugDelay);
        _delayedHugRoutine = null;
        PlayHugAndTransitionToMorning();
    }

    private void StopDelayedHug()
    {
        if (_delayedHugRoutine == null) return;
        StopCoroutine(_delayedHugRoutine);
        _delayedHugRoutine = null;
    }

    /// <summary>
    /// 시그널에서 쿠네쿠네의 TrueEnding_Idle 대기 애니메이션을 재생합니다.
    /// </summary>
    public void PlayKuneKuneTrueEndingIdle()
    {
        StopKuneKuneAdvance();
        PlayKuneKuneAnimation(kuneKuneTrueEndingIdleClip);
    }

    private bool PlayKuneKuneAnimation(AnimationClip clip)
    {
        if (!isActiveAndEnabled || kunekKune == null || clip == null)
            return false;

        Animator animator = kunekKune.GetComponent<Animator>();
        if (animator == null) return false;

        StopKuneKuneAnimation();
        kunekKune.gameObject.SetActive(true);
        _kuneKuneAnimationPlayable = AnimationPlayableUtilities.PlayClip(animator, clip, out _kuneKuneAnimationGraph);
        _kuneKuneAnimationGraph.Evaluate(0f);
        return true;
    }

    /// <summary>
    /// 시그널에서 호출하면 접근 전 대사, 걷기, 두 캐릭터의 포옹, 여운, 흰 화면, 새벽 순서로 이어집니다.
    /// </summary>
    public void PlayHugAndTransitionToMorning()
    {
        if (!isActiveAndEnabled || !IsCutsceneRunning || _hugRequested || _narrativeCompleted)
            return;

        // 기존 씬 인스턴스에 새 클립 필드가 비어 있어도 제작된 리소스로 연결합니다.
        if (playerWalkClip == null)
            playerWalkClip = Resources.Load<AnimationClip>("Animations/Player/Child/PC_Child_Walk");
        if (playerHugClip == null)
            playerHugClip = Resources.Load<AnimationClip>("Animations/Player/Child/PC_Hug");
        if (kuneKuneTrueEndingIdleClip == null)
            kuneKuneTrueEndingIdleClip = Resources.Load<AnimationClip>("Animations/KuneKune/TrueEnding_Idle");
        if (kuneKuneHugClip == null)
            kuneKuneHugClip = Resources.Load<AnimationClip>("Animations/KuneKune/TrueEnding_Hug");

        if (MapManager.Instance?.Player?.GetComponentInChildren<Animator>() == null ||
            kunekKune == null || kunekKune.GetComponent<Animator>() == null ||
            playerWalkClip == null || playerWalkClip.length <= 0f ||
            playerHugClip == null || playerHugClip.length <= 0f ||
            kuneKuneTrueEndingIdleClip == null || kuneKuneHugClip == null || kuneKuneHugClip.length <= 0f)
        {
            Debug.LogWarning("진엔딩의 배우 Animator와 PC_Child_Walk, PC_Hug, TrueEnding_Idle, TrueEnding_Hug 클립을 확인해주세요.", this);
            return;
        }

        StopDelayedHug();
        StopPlayerMovement();
        StopKuneKuneAdvance();
        UIManager.Instance?.StopTrueEndingEcho();
        _hugRequested = true;
        // 시그널 콜백에서는 Timeline을 멈춰두고, 종료 루틴에서 포옹과 새벽 전환을 한 번만 실행합니다.
        director.Pause();
        EndCutscene();
    }

    private IEnumerator PlayHugRoutine()
    {
        GameObject player = MapManager.Instance.Player;
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        Vector2 start = body != null ? body.position : (Vector2)player.transform.position;
        float direction = kunekKune.position.x < start.x ? -1f : 1f;
        movement?.SetFacingDirection(direction);
        Transform playerVisual = player.GetComponentInChildren<Animator>().transform;
        Vector3 visualOffset = playerVisual.position - player.transform.position;
        // 쿠네쿠네가 이동해도 포옹 간격을 유지하고, Visual 오프셋으로 발 높이를 맞춥니다.
        Vector2 target = new Vector2(kunekKune.position.x - direction * playerHugStopDistance - visualOffset.x,
            kunekKune.position.y - visualOffset.y);
        float approachDuration = Vector2.Distance(start, target) / Mathf.Max(0.01f, playerHugMoveSpeed);

        movement?.SetCutsceneMovementControlled(true);
        _hugPlayerMovement = movement;
        PlayKuneKuneTrueEndingIdle();
        if (!PlayPlayerAnimation(playerWalkClip)) yield break;

        // 쿠네쿠네의 위치는 유지하고, 플레이어만 PC_Child_Walk를 반복하며 일정한 속도로 접근합니다.
        float elapsed = 0f;
        while (elapsed < approachDuration)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;
            Vector2 position = Vector2.Lerp(start, target, Mathf.Clamp01(elapsed / approachDuration));
            if (body != null)
                body.MovePosition(position);
            else
                player.transform.position = new Vector3(position.x, position.y, player.transform.position.z);
        }

        // 마지막 MovePosition을 반영하고 이동을 멈춘 같은 프레임에 두 포옹 클립을 시작합니다.
        yield return new WaitForFixedUpdate();
        StopPlayerMovement();
        if (!PlayPlayerAnimation(playerHugClip) || !PlayKuneKuneAnimation(kuneKuneHugClip))
            yield break;

        // 걷기와 같은 자동 재생 경로를 사용하고, 각 클립이 끝나면 마지막 포즈를 고정합니다.
        float hugDuration = Mathf.Max(playerHugClip.length, kuneKuneHugClip.length);
        float playerLastFrameTime = Mathf.Max(0f, playerHugClip.length - 0.0001f);
        float kuneKuneLastFrameTime = Mathf.Max(0f, kuneKuneHugClip.length - 0.0001f);
        elapsed = 0f;
        while (elapsed < hugDuration)
        {
            yield return null;
            elapsed = Mathf.Min(hugDuration, elapsed + Time.deltaTime);
            if (elapsed >= playerLastFrameTime)
            {
                _playerAnimationPlayable.SetTime(playerLastFrameTime);
                _playerAnimationPlayable.SetSpeed(0f);
                _playerAnimationGraph.Evaluate(0f);
            }
            if (elapsed >= kuneKuneLastFrameTime)
            {
                _kuneKuneAnimationPlayable.SetTime(kuneKuneLastFrameTime);
                _kuneKuneAnimationPlayable.SetSpeed(0f);
                _kuneKuneAnimationGraph.Evaluate(0f);
            }
        }

        // 흰 화면이 완전히 덮일 때까지 두 캐릭터의 포옹 포즈를 유지합니다.
        yield return new WaitForSeconds(hugHoldDuration);
    }

    private IEnumerator TransitionToMorningBusstop()
    {
        StopKuneKuneAdvance();
        StopPlayerMovement();
        UIManager.Instance?.StopTrueEndingEcho();

        // 흰 화면이 올라오는 동안 피아노를 줄이고, 새벽 전에는 완전히 멈춥니다.
        if (_isEndingMusicPlaying)
            SoundManager.Instance?.StopBGM(whiteFadeDuration);
        _transitionUI = UIManager.Instance;
        if (_transitionUI != null)
        {
            _fadeColorBeforeTransition = _transitionUI.FadeColor;
            _hasFadeColorOverride = true;
            _transitionUI.FadeColor = Color.white;
            yield return _transitionUI.FadeOut(whiteFadeDuration);
        }
        else if (_isEndingMusicPlaying)
            yield return new WaitForSecondsRealtime(whiteFadeDuration);

        StopEndingMusic();

        director.Stop();
        StopPlayerAnimation();
        StopKuneKuneAnimation();
        MapManager mapManager = MapManager.Instance;
        GameObject player = mapManager.Player;
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        movement?.StopImmediately();
        movement?.SetCurrentHeat(0f, true);
        PlayerAppearance appearance = player.GetComponent<PlayerAppearance>();
        appearance?.SetSweatyState(false);
        appearance?.SetAge(EPlayerAge.Adult);

        if (kunekKune != null)
            kunekKune.gameObject.SetActive(false);
        mapManager.KuneKune?.Despawn();
        RestoreCamera();
        mapManager.InitMap(EMapType.Outside_Busstop, "outside_gamestart");

        // 이동한 카메라의 LateUpdate 후 새 위치에 하늘과 움직이는 구름을 배치합니다.
        yield return null;
        BackgroundManager.Instance?.SetMorningBackground(morningSky, brightCloudSprites);
        // 맵의 기본 배경음 차단은 유지한 채 새벽 장면의 새소리만 재생합니다.
        SoundManager.Instance?.PlayBGM("Bird", playbackOwner: this);
        yield return new WaitForSecondsRealtime(0.5f);

        if (_transitionUI != null)
            yield return _transitionUI.FadeIn(morningFadeDuration);

        RestoreTransitionFade(false);
    }

    private IEnumerator PlayEndingDialog(DialogData dialog)
    {
        DialogSystem dialogSystem = DialogSystem.Instance;
        if (dialog == null || dialogSystem == null)
        {
            Debug.LogWarning("진엔딩의 대사 에셋과 DialogSystem을 확인해주세요.", this);
            yield break;
        }

        yield return new WaitUntil(() => dialogSystem == null || !dialogSystem.IsDialogPlaying);
        if (dialogSystem == null) yield break;
        if (dialog == playerApproachDialog && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGM(EndingPianoBgmId, endingMusicFadeInDuration,
                playbackOwner: this, startTime: endingMusicStartTime, loop: false);
            _isEndingMusicPlaying = true;
        }
        dialogSystem.PlayDialog(dialog);
        yield return new WaitUntil(() => dialogSystem == null || !dialogSystem.IsDialogPlaying);
    }

    private IEnumerator FadeEndingToBlack()
    {
        UIManager ui = UIManager.Instance;
        if (ui != null)
        {
            ui.FadeColor = Color.black;
            yield return ui.FadeOut(endingFadeDuration);
        }
        _endingBlackoutCompleted = true;
    }

    protected override IEnumerator CutsceneEndRoutine()
    {
        // Timeline이 먼저 끝나도 메아리와 3초 응시 뒤의 포옹 요청까지 기다립니다.
        yield return new WaitUntil(() => !_isKuneKuneAdvancing && _delayedHugRoutine == null &&
            (DialogSystem.Instance == null || !DialogSystem.Instance.IsDialogPlaying));

        if (MapManager.Instance?.Player == null)
        {
            Debug.LogWarning("진엔딩의 새벽 전환에 필요한 실제 플레이어를 확인해주세요.", this);
            OnCutsceneEndSetup();
            CompleteCutscene();
            yield break;
        }

        if (playerApproachDialog == null)
            playerApproachDialog = Resources.Load<DialogData>("Cutscenes/Ending0/TrueEnding_BeforeApproach");
        if (playerHugDialog == null)
            playerHugDialog = Resources.Load<DialogData>("Cutscenes/Ending0/TrueEnding_10");
        if (playerEpilogueDialog == null)
            playerEpilogueDialog = Resources.Load<DialogData>("Cutscenes/Ending0/TrueEnding_11");

        if (_hugRequested)
        {
            // 마지막 대사까지 모두 끝나면 서로 바라보며 여운을 둔 뒤 다가갑니다.
            yield return PlayEndingDialog(playerApproachDialog);
            yield return new WaitForSeconds(playerApproachPause);
            yield return PlayHugRoutine();
            // 포옹 완료 후 1초 여운은 PlayHugRoutine에서 유지합니다.
            yield return PlayEndingDialog(playerHugDialog);
            // '너도..... 나도.....'를 마친 뒤에도 포옹한 채 여운을 유지합니다.
            yield return new WaitForSeconds(whiteFadeDelay);
        }
        yield return TransitionToMorningBusstop();
        yield return new WaitForSeconds(morningDialogDelay);
        yield return PlayEndingDialog(playerEpilogueDialog);
        // 대사 UI가 닫힌 뒤에는 풍경과 새소리만 3초간 유지합니다.
        yield return new WaitForSeconds(landscapeHoldDuration);
        yield return FadeEndingToBlack();
        // 문구가 나타나기 시작할 때부터 사라질 때까지 새소리를 함께 줄입니다.
        float endingTitleDuration = endingTitleFadeDuration * 2f + endingTitleHoldDuration;
        SoundManager.Instance?.StopBGM(endingTitleDuration);
        if (UIManager.Instance != null)
            yield return UIManager.Instance.ShowEndingTitle(endingTitle, endingTitleFadeDuration, endingTitleHoldDuration);
        _narrativeCompleted = true;
        OnCutsceneEndSetup();
        CompleteCutscene();
    }

    protected override void OnCutsceneFinished()
    {
        ReleaseBackgroundMusic();
        base.OnCutsceneFinished();
        onTrueEndingFinished?.Invoke();
    }

    public void GoToTitle()
    {
        if (!isActiveAndEnabled || !_narrativeCompleted || IsCutsceneRunning || _titleReturnRoutine != null)
            return;

        // 종료 이벤트 이후에도 씬 전환 동안 조작이 새지 않도록 잠급니다.
        HoldControls();
        _titleReturnRoutine = StartCoroutine(ReturnToTitle());
    }

    private IEnumerator ReturnToTitle()
    {
        // 문구가 사라진 뒤에는 소리 없는 검은 화면을 유지합니다.
        SoundManager.Instance?.StopBGM(0f);
        if (!_endingBlackoutCompleted && UIManager.Instance != null)
        {
            UIManager.Instance.FadeColor = Color.black;
            yield return UIManager.Instance.FadeOut(1.5f);
        }
        yield return new WaitForSecondsRealtime(endingBlackHoldDuration);

        Time.timeScale = 1f;
        SceneManager.LoadScene(TitleSceneName);
    }

    private void RestoreTransitionFade(bool clearScreen)
    {
        if (_hasFadeColorOverride && _transitionUI != null)
        {
            if (clearScreen)
                _transitionUI.SetFadeAlpha(0f);
            _transitionUI.FadeColor = _fadeColorBeforeTransition;
        }
        _hasFadeColorOverride = false;
        _transitionUI = null;
    }

    private void StopPlayerMovement()
    {
        if (_hugPlayerMovement != null)
        {
            _hugPlayerMovement.SetCutsceneMovementControlled(false);
            _hugPlayerMovement = null;
        }
        GameObject player = MapManager.Instance != null ? MapManager.Instance.Player : null;
        if (player != null)
            player.GetComponent<PlayerMovement>()?.StopImmediately();
    }

    private void FixedUpdate()
    {
        if (!_isKuneKuneAdvancing)
            return;

        if (kunekKune == null || !kunekKune.gameObject.activeInHierarchy)
        {
            StopKuneKuneAdvance();
            return;
        }

        if (MapManager.Instance?.Player == null)
        {
            StopKuneKuneAdvance();
            return;
        }

        Vector3 position = kunekKune.localPosition;
        position.x = Mathf.MoveTowards(position.x, kuneKuneArrivalLocalX, kuneKuneAdvanceSpeed * Time.fixedDeltaTime);
        kunekKune.localPosition = position;
        if (!Mathf.Approximately(position.x, kuneKuneArrivalLocalX))
            return;

        if (!_kuneKuneAtApproachStop)
        {
            _kuneKuneAtApproachStop = true;
            if (_kuneKuneScratchSource != null)
                _kuneKuneScratchSource.Stop();
            kunekKune.GetComponent<Animator>()?.Play("Cutscene_ToiletTrueFlag_Idle", 0, 0f);
        }
        if (_approachEchoFinished)
            CompleteKuneKuneApproach();
    }

    private void StopPlayerAnimation()
    {
        if (_playerAnimationGraph.IsValid())
            _playerAnimationGraph.Destroy();
    }

    private void StopKuneKuneAnimation()
    {
        if (_kuneKuneAnimationGraph.IsValid())
            _kuneKuneAnimationGraph.Destroy();
    }

    protected override void OnCutsceneStartSetup()
    {
        base.OnCutsceneStartSetup();
        BlockBackgroundMusic();
        _narrativeCompleted = false;
        _endingBlackoutCompleted = false;
        _hugRequested = false;
        StopDelayedHug();
#if UNITY_EDITOR
        // 시작 암전 중에 배우를 배치하여 O키를 누른 장소와 관계없이 컷씬을 확인합니다.
        if (_isEditorPreview)
            PrepareEditorPreview();
#endif
        // Timeline의 마지막 연출이 끝나면 종료 루틴에서 흰 화면과 새벽으로 이어집니다.
        director.extrapolationMode = DirectorWrapMode.None;
        if (trueEndingCamera == null)
        {
            Debug.LogWarning("진엔딩의 CM_TrueEnding 카메라를 지정해주세요.", this);
            return;
        }

        if (!_hasCameraOverride)
        {
            _cameraPriorityBeforeCutscene = trueEndingCamera.Priority;
            _cameraWasActive = trueEndingCamera.gameObject.activeSelf;
            _cameraWasEnabled = trueEndingCamera.enabled;
            _hasCameraOverride = true;
        }

        trueEndingCamera.gameObject.SetActive(true);
        trueEndingCamera.enabled = true;
        trueEndingCamera.PreviousStateIsValid = false;
        trueEndingCamera.Priority = CutsceneCameraPriority;
    }

    private void RestoreCamera()
    {
        if (!_hasCameraOverride)
            return;

        if (trueEndingCamera != null)
        {
            trueEndingCamera.Priority = _cameraPriorityBeforeCutscene;
            trueEndingCamera.enabled = _cameraWasEnabled;
            trueEndingCamera.gameObject.SetActive(_cameraWasActive);
        }

        _hasCameraOverride = false;
    }

    private void RestoreCutsceneState()
    {
        StopDelayedHug();
        StopEndingMusic();
        RestoreTransitionFade(true);
        StopPlayerMovement();
        UIManager.Instance?.StopTrueEndingEcho();
        UIManager.Instance?.HideEndingTitle();
        RestoreCamera();
        StopKuneKuneAdvance();
        StopPlayerAnimation();
        StopKuneKuneAnimation();
    }

    protected override void OnCutsceneEndSetup()
    {
        RestoreCutsceneState();
        base.OnCutsceneEndSetup();
    }

    protected override void OnDisable()
    {
        if (_dialogController != null)
            _dialogController.DialogFinished -= OnDialogFinished;
        _narrativeCompleted = false;
        _titleReturnRoutine = null;
#if UNITY_EDITOR
        _isEditorPreview = false;
#endif
        RestoreCutsceneState();
        ReleaseBackgroundMusic();
        base.OnDisable();
    }
}
