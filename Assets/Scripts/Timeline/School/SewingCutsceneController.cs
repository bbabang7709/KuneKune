using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(DialogTimelineController))]
public class SewingCutsceneController : CutsceneControllerBase
{
    [SerializeField] private CinemachineCamera cm;
    [SerializeField] private Door door;
    [SerializeField] private GameObject girl;

    [Header("소녀를 따라간 뒤의 대사")]
    [SerializeField] private DialogData gateClosedDialog;
    [SerializeField] private DialogData encounterDialog;
    [SerializeField] private DialogData lockedGateDialog;
    [SerializeField] private DialogData caughtDialog;

    [Header("산에 도착했을 때의 대사")]
    [SerializeField] private DialogData firstVisitSanDialog;

    [Header("연출 이동")]
    [SerializeField] private Transform playerWalkTarget;
    [SerializeField] private EMapType destinationMap = EMapType.Flashback;
    [SerializeField] private string destinationEntryID = "flashback_0";

    private bool _isStarting;
    private bool _isEditorPreview;
    private bool _ownsDoor;
    private bool _doorWasLocked;
    private bool _ownsCamera;
    private bool _ownsPlayerPhysics;
    private bool _playerBodyWasSimulated;
    private bool _ownsDialog;
    private bool _hasStartedPlayerWalk;
    private bool _isPlayerWalking;
    private bool _hasOpenedGate;
    private bool _ownsPowerOutage;
    private bool _isKuneApproaching;
    private Coroutine _playerWalkRoutine;
    private Coroutine _kuneApproachRoutine;
    private PrioritySettings _cameraPriorityBeforeCutscene;
    private PlayerMovement _playerMovement;
    private PlayerAnimation _playerAnimation;
    private Rigidbody2D _playerBody;
    private KuneKune _eventKuneKune;
    private DialogTimelineController _dialogController;
    private Vector3 _girlInitialPosition;
    private Vector3 _girlInitialScale;

    protected override void Awake()
    {
        base.Awake();
        // 기존 프리팹에도 이 Timeline 전용 대사 컨트롤러를 보장합니다.
        if (GetComponent<DialogTimelineController>() == null)
            gameObject.AddComponent<DialogTimelineController>();
        _dialogController = GetComponent<DialogTimelineController>();
        if (girl == null) girl = transform.Find("Girl")?.gameObject;
        if (girl != null)
        {
            _girlInitialPosition = girl.transform.localPosition;
            _girlInitialScale = girl.transform.localScale;
        }
        if (gateClosedDialog == null) gateClosedDialog = Resources.Load<DialogData>("Dialogs/School/Follow_Girl_2-3");
        if (encounterDialog == null) encounterDialog = Resources.Load<DialogData>("Dialogs/School/Follow_Girl_4");
        if (lockedGateDialog == null) lockedGateDialog = Resources.Load<DialogData>("Dialogs/School/Follow_Girl_5");
        if (caughtDialog == null) caughtDialog = Resources.Load<DialogData>("Dialogs/School/Follow_Girl_6");
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _dialogController.DialogStarted += OnTimelineDialogStarted;
        _dialogController.DialogFinished += OnTimelineDialogFinished;
    }

    private void OnTimelineDialogStarted(DialogData dialog) => _ownsDialog = true;
    private void OnTimelineDialogFinished(DialogData dialog) => _ownsDialog = false;

    public override void PlayCutscene()
    {
        if (_isStarting || IsCutsceneRunning || !isActiveAndEnabled || (playOnlyOnce && hasPlayed)) return;
        _isStarting = true;
        StartCoroutine(WaitAndPlaySequence());
    }

    private IEnumerator WaitAndPlaySequence()
    {
        // 인형 완성 직후에는 사물함이 완성 이미지를 보여주고 UI를 닫을 때까지 기다립니다.
        yield return new WaitUntil(() =>
            Time.timeScale > 0f &&
            (SaveManager.Instance == null || !SaveManager.Instance.IsLoading) &&
            (DialogSystem.Instance == null || !DialogSystem.Instance.IsDialogPlaying) &&
            (CutsceneManager.Instance == null || !CutsceneManager.Instance.IsCutscenePlaying) &&
            (ControlLockManager.Instance == null ||
                (!ControlLockManager.Instance.IsInputLocked && !ControlLockManager.Instance.IsMovementLocked)));

        if (!ValidateSequence())
        {
            _isStarting = false;
            _isEditorPreview = false;
            yield break;
        }

        HoldControls();
        director.Stop();
        director.time = 0d;
        base.PlayCutscene();
        _isStarting = false;
    }

    private bool ValidateSequence()
    {
        if (MapManager.Instance?.Player == null || MapManager.Instance.KuneKune == null ||
            CutsceneManager.Instance == null || DialogSystem.Instance == null || LightingEventManager.Instance == null ||
            director.playableAsset == null || door == null || girl == null ||
            playerWalkTarget == null || MapManager.Instance.Player.GetComponent<PlayerMovement>() == null ||
            gateClosedDialog == null || encounterDialog == null || lockedGateDialog == null || caughtDialog == null)
        {
            Debug.LogError("[SewingCutscene] 플레이어, 문, 소녀, 대사와 컷씬 매니저 연결을 확인해주세요.", this);
            return false;
        }

        foreach (MapArea area in FindObjectsByType<MapArea>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (area.mapType == destinationMap && area.GetEntryPoint(destinationEntryID) != null)
                return true;
        }

        Debug.LogError($"[SewingCutscene] 도착 맵 {destinationMap}의 {destinationEntryID} 진입점이 필요합니다.", this);
        return false;
    }

    protected override void OnCutsceneStartSetup()
    {
        // Timeline의 이동 결과가 다음 K 미리보기의 시작 위치에 누적되지 않게 합니다.
        girl.transform.localPosition = _girlInitialPosition;
        girl.transform.localScale = _girlInitialScale;
        _hasStartedPlayerWalk = false;
        _isPlayerWalking = false;
        _hasOpenedGate = false;
        MapManager.Instance.KuneKune.Despawn();
        MapManager.Instance.InitMap(EMapType.School_Hallway, "school_hallway_0");
        GameObject player = MapManager.Instance.Player;
        player.GetComponent<PlayerAppearance>()?.SetAge(EPlayerAge.Student);
        player.GetComponent<PlayerAppearance>()?.SetSweatyState(false);
        _playerMovement = player.GetComponent<PlayerMovement>();
        _playerAnimation = player.GetComponentInChildren<PlayerAnimation>();
        _playerBody = player.GetComponent<Rigidbody2D>();
        _playerMovement?.SetCurrentHeat(0f, true);
        _playerMovement?.SetFacingDirection(1f);

        // 소녀를 발견하는 위치에서 시작하며, 학교 맵의 바닥 높이는 유지합니다.
        Vector3 position = player.transform.position;
        position.x = girl.transform.position.x - 2f;
        player.transform.position = position;
        // 횡스크롤 복도의 목표 지점을 학생 플레이어와 같은 바닥 높이에 맞춥니다.
        Vector3 targetPosition = playerWalkTarget.position;
        targetPosition.y = position.y;
        targetPosition.z = position.z;
        playerWalkTarget.position = targetPosition;
        Physics2D.SyncTransforms();

        _doorWasLocked = door.IsLocked;
        _ownsDoor = true;
        door.SetEventLocked(true);
        door.SetLockState(false);
        if (cm != null)
        {
            _cameraPriorityBeforeCutscene = cm.Priority;
            _ownsCamera = true;
        }
        BackgroundManager.Instance?.SetBackgroundForChapter(EChapter.Chapter3_School);
    }

    protected override IEnumerator CutsceneEndRoutine()
    {
        // Timeline이 끝나도 컷씬 잠금은 뒷산 도착까지 유지합니다.
        HoldControls();
        RestoreCamera();
        // 시그널에서 시작한 이동이 타임라인보다 길면 도착까지 기다립니다.
        yield return new WaitUntil(() => !_isPlayerWalking);
        girl.SetActive(false);
        door.SetCutsceneOpen(false);
        door.SetLockState(true);
        SoundManager.Instance?.PlaySFX("StallDoor_Open");
        yield return PlayDialogAndWait(gateClosedDialog);

        _playerMovement?.SetFacingDirection(-1f);
        _ownsPowerOutage = true;
        SoundManager.Instance?.PlaySFX("Fluorescent_Flicker");
        yield return LightingEventManager.Instance.PowerOutageRoutine(SpawnEventKuneKune);
        _ownsPowerOutage = false;
        _isKuneApproaching = true;
        _kuneApproachRoutine = StartCoroutine(ApproachPlayer());
        yield return PlayDialogAndWait(encounterDialog);

        _playerMovement?.SetFacingDirection(1f);
        SoundManager.Instance?.PlaySFX("Door_Banging");
        yield return PlayDialogAndWait(lockedGateDialog);

        _playerMovement?.SetFacingDirection(-1f);
        yield return new WaitUntil(() => !_isKuneApproaching);
        SoundManager.Instance?.PlaySFX("KuneKune_Jumpscare");
        yield return UIManager.Instance?.FadeOutKeepingDialog(0.5f);
        yield return PlayDialogAndWait(caughtDialog);

        StopEventKuneKune();
        MapManager.Instance.InitMap(destinationMap, destinationEntryID);
        MapManager.Instance.Player.GetComponent<PlayerAppearance>()?.SetAge(EPlayerAge.Child);
        _playerMovement?.SetCurrentHeat(0f, true);
        BackgroundManager.Instance?.SetBackgroundForChapter(EChapter.Chapter4_Flashback);

        yield return new WaitForSeconds(4f);
        yield return UIManager.Instance?.FadeIn(1.5f);
        yield return UIManager.Instance?.FadeInGameplayUI(1f);

        // 에디터 미리보기는 실제 플레이의 체크포인트를 덮어쓰지 않습니다.
        if (!_isEditorPreview) SaveManager.Instance?.SaveChapterCheckpoint(EChapter.Chapter4_Flashback);
        door.SetEventLocked(false);
        _ownsDoor = false;
        _isEditorPreview = false;
        ReleaseControls();
        CompleteCutscene();

        if (DialogSystem.Instance != null && firstVisitSanDialog != null) 
            DialogSystem.Instance.PlayDialog(firstVisitSanDialog);
    }

    /// <summary>Timeline 시그널에서 호출합니다. 학생의 걷기 속도로 지정된 목표까지 이동합니다.</summary>
    public void WalkPlayerToTarget()
    {
        if (!IsCutsceneRunning || _hasStartedPlayerWalk || playerWalkTarget == null || _playerMovement == null) return;
        _hasStartedPlayerWalk = true;
        _isPlayerWalking = true;
        _playerWalkRoutine = StartCoroutine(MovePlayerToTarget());
    }

    /// <summary>철문이 열리는 Timeline 시그널에서 호출합니다.</summary>
    public void OpenGate()
    {
        if (!IsCutsceneRunning || _hasOpenedGate || door == null) return;
        _hasOpenedGate = true;
        door.SetCutsceneOpen(true);
        SoundManager.Instance?.PlaySFX("StallDoor_Open", 3f);
    }

    private IEnumerator MovePlayerToTarget()
    {
        Transform player = MapManager.Instance.Player.transform;
        _playerMovement.SetFacingDirection(Mathf.Sign(playerWalkTarget.position.x - player.position.x));
        _playerMovement?.SetCutsceneMovementControlled(true);
        _playerAnimation?.SetCutsceneWalking(true);
        // 연출 경로의 사물 충돌이나 트리거가 이동을 막거나 다른 이벤트를 시작하지 않게 합니다.
        if (_playerBody != null)
        {
            _playerBodyWasSimulated = _playerBody.simulated;
            _ownsPlayerPhysics = true;
            _playerBody.simulated = false;
        }
        while (Vector3.Distance(player.position, playerWalkTarget.position) > 0.05f)
        {
            yield return new WaitForFixedUpdate();
            player.position = Vector3.MoveTowards(player.position, playerWalkTarget.position,
                Mathf.Max(0.1f, _playerMovement.WalkingSpeed) * Time.fixedDeltaTime);
        }
        player.position = playerWalkTarget.position;
        StopPlayerMovement();
    }

    private void SpawnEventKuneKune()
    {
        GameObject player = MapManager.Instance.Player;
        Vector3 position = player.transform.position + Vector3.left * 6f;
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider != null) position.y = playerCollider.bounds.min.y;
        _eventKuneKune = MapManager.Instance.KuneKune;
        _eventKuneKune.Spawn(position, true);
        // 일반 사망/저장 로드를 막으며 애니메이션은 Run을 그대로 재생합니다.
        _eventKuneKune.State = ECharacterState.Idle;
        Animator animator = _eventKuneKune.GetComponent<Animator>();
        if (animator != null)
        {
            animator.speed = 1f;
            animator.Play("Run", 0, 0f);
            animator.Update(0f);
        }
    }

    private IEnumerator ApproachPlayer()
    {
        Transform player = MapManager.Instance.Player.transform;
        Animator animator = _eventKuneKune.GetComponent<Animator>();
        if (animator != null) animator.speed = 1f;
        while (Mathf.Abs(_eventKuneKune.transform.position.x - player.position.x) > 0.3f)
        {
            if (DialogSystem.Instance != null && DialogSystem.Instance.IsDialogPlaying)
            {
                yield return null;
                continue;
            }
            Vector3 position = _eventKuneKune.transform.position;
            position.x = Mathf.MoveTowards(position.x, player.position.x, KuneKune.DefaultMoveSpeed * Time.deltaTime);
            _eventKuneKune.transform.position = position;
            yield return null;
        }
        if (animator != null) animator.speed = 0f;
        _isKuneApproaching = false;
    }

    private IEnumerator PlayDialogAndWait(DialogData dialog)
    {
        yield return new WaitUntil(() => DialogSystem.Instance == null || !DialogSystem.Instance.IsDialogPlaying);
        if (DialogSystem.Instance == null) yield break;
        _ownsDialog = true;
        DialogSystem.Instance.PlayDialog(dialog);
        yield return new WaitUntil(() => DialogSystem.Instance == null || !DialogSystem.Instance.IsDialogPlaying);
        _ownsDialog = false;
    }

    private void StopPlayerMovement()
    {
        _isPlayerWalking = false;
        _playerWalkRoutine = null;
        if (_ownsPlayerPhysics)
        {
            if (_playerBody != null) _playerBody.simulated = _playerBodyWasSimulated;
            _ownsPlayerPhysics = false;
            Physics2D.SyncTransforms();
        }
        _playerMovement?.SetCutsceneMovementControlled(false);
        _playerAnimation?.SetCutsceneWalking(false);
        _playerAnimation?.UpdateAnimation(false, false, false, false);
    }

    private void StopEventKuneKune()
    {
        if (_kuneApproachRoutine != null) StopCoroutine(_kuneApproachRoutine);
        _kuneApproachRoutine = null;
        _isKuneApproaching = false;
        if (_eventKuneKune == null) return;
        Animator animator = _eventKuneKune.GetComponent<Animator>();
        if (animator != null) animator.speed = 1f;
        _eventKuneKune.Despawn();
        _eventKuneKune = null;
    }

    private void RestoreCamera()
    {
        if (!_ownsCamera) return;
        if (cm != null) cm.Priority = _cameraPriorityBeforeCutscene;
        _ownsCamera = false;
    }

    protected override void OnDisable()
    {
        bool wasRunning = IsCutsceneRunning;
        if (_dialogController != null)
        {
            _dialogController.DialogStarted -= OnTimelineDialogStarted;
            _dialogController.DialogFinished -= OnTimelineDialogFinished;
        }
        if (_ownsDialog && DialogSystem.Instance != null) DialogSystem.Instance.EndDialog();
        _ownsDialog = false;
        if (_playerWalkRoutine != null) StopCoroutine(_playerWalkRoutine);
        base.OnDisable();
        if (_ownsPowerOutage) LightingEventManager.Instance?.RestoreAfterPowerOutage();
        _ownsPowerOutage = false;
        StopPlayerMovement();
        StopEventKuneKune();
        RestoreCamera();
        if (_ownsDoor && door != null)
        {
            door.SetCutsceneOpen(false);
            door.SetLockState(_doorWasLocked);
            door.SetEventLocked(false);
        }
        _ownsDoor = false;
        _isStarting = false;
        _isEditorPreview = false;
        if (wasRunning) CutsceneManager.Instance?.EndCutscene();
    }

    private void LateUpdate()
    {
#if UNITY_EDITOR
        if (Keyboard.current == null || !Keyboard.current.kKey.wasPressedThisFrame ||
            _isStarting || IsCutsceneRunning || Time.timeScale <= 0f ||
            (SaveManager.Instance != null && SaveManager.Instance.IsLoading) ||
            (CutsceneManager.Instance != null && CutsceneManager.Instance.IsCutscenePlaying) ||
            (DialogSystem.Instance != null && DialogSystem.Instance.IsDialogPlaying) ||
            (ControlLockManager.Instance != null &&
                (ControlLockManager.Instance.IsInputLocked || ControlLockManager.Instance.IsMovementLocked))) return;

        hasPlayed = false;
        _isEditorPreview = true;
        PlayCutscene();
#endif
    }
}
