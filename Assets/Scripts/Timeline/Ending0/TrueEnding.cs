using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using Unity.Cinemachine;

[RequireComponent(typeof(DialogTimelineController))]
public class TrueEnding : CutsceneControllerBase
{
    private const int CutsceneCameraPriority = 20;
    private const string TitleSceneName = "Scene_Intro";
    private static readonly string[] ApproachEchoPhrases =
    {
        "가지 마……",
        "그때는……",
        "곁에 있어줬잖아……",
        "나 아직 아파……",
        "아직도…… 아파……"
    };
    private static readonly string[] PainEchoPhrases =
    {
        "아파……",
        "무서워……",
        "외로워……"
    };
    private static readonly string[] LonelyEchoPhrases = { "외로워……" };

    [SerializeField] private AnimationClip playerSweatIdleClip;
    [SerializeField] private AnimationClip playerSweatIdleSideClip;
    [SerializeField] private AnimationClip playerSweatWalkClip;
    [SerializeField] private DialogData firstDialog;
    [SerializeField] private DialogData playerApproachDialog;
    [SerializeField] private DialogData playerPromiseDialog;
    [SerializeField] private DialogData playerUnderstandingDialog;
    [SerializeField] private DialogData playerEmpathyDialog;
    [SerializeField] private DialogData playerInvitationDialog;
    [SerializeField] private DialogData playerConfessionDialog;
    [SerializeField] private DialogData playerReflectionDialog;
    [SerializeField] private DialogData playerDenialDialog;
    [SerializeField] private DialogData playerAcceptanceDialog;
    [SerializeField] private DialogData playerDepartureDialog;
    [SerializeField] private DialogData playerEpilogueDialog;
    [SerializeField] private UnityEvent onTrueEndingFinished = new UnityEvent();
    [SerializeField] private Sprite morningSky;
    [SerializeField] private Sprite[] brightCloudSprites;

    [SerializeField] Transform kunekKune;
    [SerializeField, Min(0f)] private float playerApproachDelay = 0.5f;
    [SerializeField, Min(0.01f)] private float playerApproachSpeed = 1f;
    [SerializeField, Min(0f)] private float playerApproachStopDistance = 3f;
    [SerializeField, Min(0.01f)] private float playerWalkAnimationSpeed = 0.5f;
    [SerializeField, Min(0f)] private float laughingWomenGain = 10f;
    [SerializeField] private CinemachineCamera trueEndingCamera;

    private PlayableGraph _playerAnimationGraph;
    private Coroutine _delayedDialogRoutine;
    private Coroutine _memorySoundRoutine;
    private Coroutine _busstopTransitionRoutine;
    private Coroutine _titleReturnRoutine;
    private bool _narrativeCompleted;
    private UIManager _transitionUI;
    private Color _fadeColorBeforeTransition;
    private bool _hasFadeColorOverride;
    private AudioSource _memorySoundSource;
    private AudioSource _kuneKuneScratchSource;
    private SoundManager _bgmSoundManager;
    private DialogTimelineController _dialogController;
    private bool _isKuneKuneAdvancing;
    private float _kuneKuneAdvanceDirection;
    private float _playerApproachWaitRemaining;
    private bool _isPlayerApproaching;
    private bool _isPlayerWalking;
    private PlayerMovement _approachingPlayerMovement;
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
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _dialogController.DialogFinished += OnDialogFinished;
    }

    public override void PlayCutscene()
    {
        base.PlayCutscene();
        if (IsCutsceneRunning)
            BlockBackgroundMusic();
    }

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
        if (!isActiveAndEnabled)
            return;

        GameObject player = MapManager.Instance != null ? MapManager.Instance.Player : null;
        Animator playerAnimator = player != null ? player.GetComponentInChildren<Animator>() : null;
        if (playerAnimator == null || playerSweatIdleClip == null)
        {
            Debug.LogWarning("진엔딩의 실제 플레이어 Animator와 땀 흘리는 대기 클립을 확인해주세요.", this);
            return;
        }

        StopPlayerAnimation();

        // 이동·탈진 파라미터에 따른 상태 전환 대신 지정한 클립을 직접 출력합니다.
        AnimationPlayableUtilities.PlayClip(playerAnimator, playerSweatIdleClip, out _playerAnimationGraph);
        _playerAnimationGraph.Evaluate(0f);
    }

    public void PlayPlayerSweatIdleSide()
    {
        if (!isActiveAndEnabled)
            return;

        GameObject player = MapManager.Instance != null ? MapManager.Instance.Player : null;
        Animator playerAnimator = player != null ? player.GetComponentInChildren<Animator>() : null;
        if (playerAnimator == null || playerSweatIdleSideClip == null)
        {
            Debug.LogWarning("진엔딩의 실제 플레이어 Animator와 땀 흘리는 대기 클립을 확인해주세요.", this);
            return;
        }

        StopPlayerAnimation();

        player.GetComponent<PlayerMovement>()?.SetFacingDirection(-1f);

        // 이동·탈진 파라미터에 따른 상태 전환 대신 지정한 클립을 직접 출력합니다.
        AnimationPlayableUtilities.PlayClip(playerAnimator, playerSweatIdleSideClip, out _playerAnimationGraph);
        _playerAnimationGraph.Evaluate(0f);
    }

    /// <summary>
    /// 타임라인 시그널에서 쿠네쿠네의 대사가 끝날 때까지 처음 정한 방향으로 직진시킵니다.
    /// </summary>
    public void MoveKuneKuneForward()
    {
        if (!isActiveAndEnabled)
            return;

        GameObject player = MapManager.Instance != null ? MapManager.Instance.Player : null;
        if (kunekKune == null || player == null)
        {
            Debug.LogWarning("진엔딩의 쿠네쿠네와 실제 플레이어를 확인해주세요.", this);
            return;
        }

        StopPlayerApproach();
        StopDelayedDialog();
        StopMemorySounds();
        kunekKune.gameObject.SetActive(true);
        Animator animator = kunekKune.GetComponent<Animator>();
        if (animator != null)
            animator.Play("Cutscene_ToiletTrueFlag", 0, 0f);

        SpriteRenderer spriteRenderer = kunekKune.GetComponent<SpriteRenderer>();
        _kuneKuneAdvanceDirection = player.transform.position.x < kunekKune.position.x ? -1f : 1f;
        if (spriteRenderer != null)
            spriteRenderer.flipX = true;

        _isKuneKuneAdvancing = true;
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
        _kuneKuneScratchSource?.Stop();
    }

    private void OnApproachEchoCompleted()
    {
        StopKuneKuneAdvance();
        if (!isActiveAndEnabled)
            return;

        Animator animator = kunekKune != null ? kunekKune.GetComponent<Animator>() : null;
        if (animator != null)
            animator.Play("Cutscene_ToiletTrueFlag_Idle", 0, 0f);

        SpriteRenderer spriteRenderer = kunekKune != null ? kunekKune.GetComponent<SpriteRenderer>() : null;
        if (spriteRenderer != null)
            spriteRenderer.flipX = true;

        MapManager.Instance?.Player?.GetComponent<PlayerMovement>()?.StopImmediately();
        _playerApproachWaitRemaining = playerApproachDelay;
        _isPlayerApproaching = true;
    }

    private void UpdatePlayerApproach()
    {
        if (!_isPlayerApproaching)
            return;

        GameObject player = MapManager.Instance != null ? MapManager.Instance.Player : null;
        if (player == null || kunekKune == null || !kunekKune.gameObject.activeInHierarchy)
        {
            StopPlayerApproach();
            return;
        }

        if (_playerApproachWaitRemaining > 0f)
        {
            _playerApproachWaitRemaining = Mathf.Max(0f, _playerApproachWaitRemaining - Time.fixedDeltaTime);
            return;
        }

        float xDifference = kunekKune.position.x - player.transform.position.x;
        if (Mathf.Abs(xDifference) <= playerApproachStopDistance + 0.01f)
        {
            StopPlayerApproach();
            player.GetComponent<PlayerMovement>()?.SetFacingDirection(xDifference);
            DialogSystem.Instance?.PlayDialog(playerApproachDialog, OnPlayerApproachDialogFinished);
            return;
        }

        if (!_isPlayerWalking)
        {
            Animator animator = player.GetComponentInChildren<Animator>();
            if (animator == null || playerSweatWalkClip == null)
            {
                Debug.LogWarning("진엔딩의 플레이어 걷기 애니메이션을 확인해주세요.", this);
                StopPlayerApproach();
                return;
            }

            StopPlayerAnimation();
            var playable = AnimationPlayableUtilities.PlayClip(animator, playerSweatWalkClip, out _playerAnimationGraph);
            playable.SetSpeed(playerWalkAnimationSpeed);
            _playerAnimationGraph.Evaluate(0f);
            _approachingPlayerMovement = player.GetComponent<PlayerMovement>();
            _approachingPlayerMovement?.SetCutsceneMovementControlled(true);
            _isPlayerWalking = true;
        }

        float direction = Mathf.Sign(xDifference);
        player.GetComponent<PlayerMovement>()?.SetFacingDirection(direction);
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        Vector2 position = body != null ? body.position : (Vector2)player.transform.position;
        Vector2 target = new Vector2(kunekKune.position.x - direction * playerApproachStopDistance, position.y);
        Vector2 nextPosition = Vector2.MoveTowards(position, target, playerApproachSpeed * Time.fixedDeltaTime);
        // 컷씬의 조작 잠금은 유지하면서 물리 스텝에서 실제 플레이어만 이동합니다.
        if (body != null)
            body.MovePosition(nextPosition);
        else
            player.transform.position = new Vector3(nextPosition.x, nextPosition.y, player.transform.position.z);
    }

    private void OnPlayerApproachDialogFinished()
    {
        StartDelayedDialog(playerPromiseDialog, 1f, OnPlayerPromiseDialogFinished);
    }

    private void StartDelayedDialog(DialogData dialog, float delay, System.Action onFinished = null)
    {
        if (!isActiveAndEnabled || !IsCutsceneRunning)
            return;

        StopDelayedDialog();
        _delayedDialogRoutine = StartCoroutine(PlayDelayedDialog(dialog, delay, onFinished));
    }

    private IEnumerator PlayDelayedDialog(DialogData dialog, float delay, System.Action onFinished)
    {
        yield return new WaitForSeconds(delay);
        _delayedDialogRoutine = null;
        if (isActiveAndEnabled && IsCutsceneRunning)
            DialogSystem.Instance?.PlayDialog(dialog, onFinished);
    }

    private void OnPlayerPromiseDialogFinished()
    {
        if (!isActiveAndEnabled || !IsCutsceneRunning)
            return;

        UIManager.Instance?.PlayTrueEndingEcho(PainEchoPhrases, OnPainEchoCompleted);
    }

    private void OnPainEchoCompleted()
    {
        if (isActiveAndEnabled && IsCutsceneRunning)
            DialogSystem.Instance?.PlayDialog(playerUnderstandingDialog, OnPlayerUnderstandingDialogFinished);
    }

    private void OnPlayerUnderstandingDialogFinished()
    {
        StartDelayedDialog(playerEmpathyDialog, 1f, OnPlayerEmpathyDialogFinished);
    }

    private void OnPlayerEmpathyDialogFinished()
    {
        StartDelayedDialog(playerInvitationDialog, 2f, OnPlayerInvitationDialogFinished);
    }

    private void OnPlayerInvitationDialogFinished()
    {
        StartDelayedDialog(playerConfessionDialog, 0.5f, OnPlayerConfessionDialogFinished);
    }

    private void OnPlayerConfessionDialogFinished()
    {
        StartDelayedDialog(playerReflectionDialog, 1f, OnPlayerReflectionDialogFinished);
    }

    private void OnPlayerReflectionDialogFinished()
    {
        if (!isActiveAndEnabled || !IsCutsceneRunning)
            return;

        StopMemorySounds();
        _memorySoundRoutine = StartCoroutine(PlayMemorySounds());
    }

    private IEnumerator PlayMemorySounds()
    {
        SoundManager soundManager = SoundManager.Instance;
        if (soundManager != null && _memorySoundSource == null)
            _memorySoundSource = soundManager.CreateSFXSource(transform);

        soundManager?.PlaySFXOnSource(_memorySoundSource, "Hit");
        yield return new WaitForSeconds(4f);

        // 같은 채널을 사용해 Hit를 끊고 웃음소리로 전환합니다.
        if (soundManager != null && soundManager.PlaySFXOnSource(_memorySoundSource, "Laughing_Women"))
        {
            AudioClip laughter = _memorySoundSource.clip;
            _memorySoundSource.Stop();
            // 원음의 앞부분이 작으므로 이 컷씬에서만 신호를 증폭합니다.
            _memorySoundSource.PlayOneShot(laughter, laughingWomenGain);
        }
        yield return new WaitForSeconds(4f);

        _memorySoundSource?.Stop();
        _memorySoundRoutine = null;
        if (isActiveAndEnabled && IsCutsceneRunning)
            DialogSystem.Instance?.PlayDialog(playerDenialDialog, OnPlayerDenialDialogFinished);
    }

    private void OnPlayerDenialDialogFinished()
    {
        if (!isActiveAndEnabled || !IsCutsceneRunning)
            return;

        UIManager.Instance?.PlayTrueEndingEcho(LonelyEchoPhrases, OnLonelyEchoCompleted);
    }

    private void OnLonelyEchoCompleted()
    {
        if (isActiveAndEnabled && IsCutsceneRunning)
            DialogSystem.Instance?.PlayDialog(playerAcceptanceDialog, OnPlayerAcceptanceDialogFinished);
    }

    private void OnPlayerAcceptanceDialogFinished()
    {
        StartDelayedDialog(playerDepartureDialog, 1f, OnPlayerDepartureDialogFinished);
    }

    private void OnPlayerDepartureDialogFinished()
    {
        if (!isActiveAndEnabled || !IsCutsceneRunning || _busstopTransitionRoutine != null)
            return;

        if (MapManager.Instance == null || MapManager.Instance.Player == null)
        {
            Debug.LogWarning("진엔딩의 버스 정류장 이동에 필요한 실제 플레이어를 확인해주세요.", this);
            return;
        }

        _busstopTransitionRoutine = StartCoroutine(TransitionToMorningBusstop());
    }

    private IEnumerator TransitionToMorningBusstop()
    {
        StopKuneKuneAdvance();
        StopPlayerApproach();
        StopPlayerAnimation();
        StopMemorySounds();
        UIManager.Instance?.StopTrueEndingEcho();

        _transitionUI = UIManager.Instance;
        if (_transitionUI != null)
        {
            _fadeColorBeforeTransition = _transitionUI.FadeColor;
            _hasFadeColorOverride = true;
            _transitionUI.FadeColor = Color.white;
            yield return _transitionUI.FadeOut(1f);
        }

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
            yield return _transitionUI.FadeIn(1f);

        RestoreTransitionFade(false);
        _busstopTransitionRoutine = null;
        // 흰 화면이 완전히 걷힌 뒤 새벽 하늘을 3초 동안 보여줍니다.
        StartDelayedDialog(playerEpilogueDialog, 3f, OnPlayerEpilogueDialogFinished);
    }

    private void OnPlayerEpilogueDialogFinished()
    {
        if (!isActiveAndEnabled || !IsCutsceneRunning || _narrativeCompleted)
            return;

        _narrativeCompleted = true;
        EndCutscene();
    }

    protected override void EndCutscene()
    {
        // 타임라인 길이와 무관하게 마지막 대사까지 모든 연출을 유지합니다.
        if (!_narrativeCompleted)
            return;

        base.EndCutscene();
    }

    protected override IEnumerator CutsceneEndRoutine()
    {
        OnCutsceneEndSetup();
        director.Stop();
        CompleteCutscene();
        yield break;
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
        SoundManager.Instance?.StopBGM(1.5f);
        if (UIManager.Instance != null)
            yield return UIManager.Instance.FadeOut(1.5f);

        yield return new WaitForSecondsRealtime(1f);

        Time.timeScale = 1f;
        SceneManager.LoadScene(TitleSceneName);
    }

    private void StopTitleReturn()
    {
        if (_titleReturnRoutine == null)
            return;

        StopCoroutine(_titleReturnRoutine);
        _titleReturnRoutine = null;
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

    private void StopBusstopTransition()
    {
        if (_busstopTransitionRoutine != null)
        {
            StopCoroutine(_busstopTransitionRoutine);
            _busstopTransitionRoutine = null;
        }
        RestoreTransitionFade(true);
    }

    private void StopMemorySounds()
    {
        if (_memorySoundRoutine != null)
        {
            StopCoroutine(_memorySoundRoutine);
            _memorySoundRoutine = null;
        }

        _memorySoundSource?.Stop();
    }

    private void StopDelayedDialog()
    {
        if (_delayedDialogRoutine == null)
            return;

        StopCoroutine(_delayedDialogRoutine);
        _delayedDialogRoutine = null;
    }

    private void StopPlayerApproach()
    {
        if (_approachingPlayerMovement != null)
        {
            _approachingPlayerMovement.SetCutsceneMovementControlled(false);
            _approachingPlayerMovement = null;
        }
        if (_isPlayerApproaching)
            MapManager.Instance?.Player?.GetComponent<PlayerMovement>()?.StopImmediately();
        if (_isPlayerWalking)
            PlayPlayerSweatIdleSide();
        _isPlayerApproaching = false;
        _isPlayerWalking = false;
        _playerApproachWaitRemaining = 0f;
    }

    private void FixedUpdate()
    {
        UpdatePlayerApproach();
        if (!_isKuneKuneAdvancing)
            return;

        if (kunekKune == null || !kunekKune.gameObject.activeInHierarchy)
        {
            StopKuneKuneAdvance();
            return;
        }

        kunekKune.position += Vector3.right *
            (_kuneKuneAdvanceDirection * KuneKune.DefaultMoveSpeed * 0.2f * Time.fixedDeltaTime);
    }

    private void StopPlayerAnimation()
    {
        if (_playerAnimationGraph.IsValid())
            _playerAnimationGraph.Destroy();
    }

    protected override void OnCutsceneStartSetup()
    {
        base.OnCutsceneStartSetup();
        BlockBackgroundMusic();
        _narrativeCompleted = false;
        // 타임라인 끝에서도 마지막 대사까지 모든 후속 연출을 유지합니다.
        director.extrapolationMode = DirectorWrapMode.Hold;
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

    protected override void OnCutsceneEndSetup()
    {
        StopTitleReturn();
        StopBusstopTransition();
        StopMemorySounds();
        StopDelayedDialog();
        StopPlayerApproach();
        UIManager.Instance?.StopTrueEndingEcho();
        RestoreCamera();
        StopKuneKuneAdvance();
        StopPlayerAnimation();
        base.OnCutsceneEndSetup();
    }

    protected override void OnDisable()
    {
        ReleaseBackgroundMusic();
        StopTitleReturn();
        _narrativeCompleted = false;
        StopBusstopTransition();
        StopMemorySounds();
        StopDelayedDialog();
        StopPlayerApproach();
        UIManager.Instance?.StopTrueEndingEcho();
        RestoreCamera();
        StopKuneKuneAdvance();
        if (_dialogController != null)
            _dialogController.DialogFinished -= OnDialogFinished;
        StopPlayerAnimation();
        base.OnDisable();
    }
}
