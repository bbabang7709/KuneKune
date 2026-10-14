using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(DialogTimelineController))]
public class Cutscene_TeddyBear : CutsceneControllerBase
{
    [SerializeField] private Transform teddy;
    [SerializeField] private TeleportTrigger portal;
    [SerializeField] private DialogData activationDialog;

    [Header("Teddy Shake")]
    [SerializeField, Min(0.01f)] private float shakeDuration = 1.5f;
    [SerializeField, Min(0f)] private float shakeAmplitude = 0.03f;
    [SerializeField, Min(0f)] private float shakeFrequency = 4f;

    private DialogTimelineController _dialogController;
    private Coroutine _sequenceRoutine;
    private Vector3 _teddyStartPosition;
    private PlayerMovement _playerMovement;
    private PlayerAnimation _playerAnimation;
    private Rigidbody2D _playerBody;
    private bool _isPlayerRunning;

    protected override void Awake()
    {
        base.Awake();
        _dialogController = GetComponent<DialogTimelineController>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _dialogController.DialogFinished += OnDialogFinished;
        if (portal != null) portal.PlayerEntered += OnPortalEntered;
    }

    public override void PlayCutscene()
    {
        if (IsCutsceneRunning || (playOnlyOnce && hasPlayed)) return;

        GameObject player = MapManager.Instance?.Player;
        _playerMovement = player != null ? player.GetComponent<PlayerMovement>() : null;
        _playerAnimation = player != null ? player.GetComponentInChildren<PlayerAnimation>() : null;
        _playerBody = player != null ? player.GetComponent<Rigidbody2D>() : null;
        Animator teddyAnimator = teddy != null ? teddy.GetComponent<Animator>() : null;
        if (teddyAnimator != null && teddyAnimator.runtimeAnimatorController == null)
            teddyAnimator.runtimeAnimatorController =
                Resources.Load<RuntimeAnimatorController>("Animations/KuneKune/KuneKuneAnimator");

        List<string> missingConnections = new List<string>();
        if (_playerMovement == null) missingConnections.Add("플레이어 PlayerMovement 없음");
        else if (_playerMovement.SprintingSpeed <= 0f) missingConnections.Add("플레이어 달리기 속도가 0 이하");
        if (_playerAnimation == null) missingConnections.Add("플레이어 PlayerAnimation 없음");
        if (_playerBody == null) missingConnections.Add("플레이어 Rigidbody2D 없음");
        else if (!_playerBody.simulated) missingConnections.Add("플레이어 Rigidbody2D의 Simulated가 꺼짐");
        if (activationDialog == null) missingConnections.Add("Activation Dialog 미지정");
        if (teddy == null) missingConnections.Add("Teddy 미지정");
        else if (teddyAnimator == null) missingConnections.Add("Teddy Animator 없음");
        else if (teddyAnimator.runtimeAnimatorController == null)
            missingConnections.Add("Teddy Animator Controller 없음 (KuneKuneAnimator 로드 실패)");
        if (portal == null) missingConnections.Add("Portal 미지정");
        else
        {
            if (!portal.isActiveAndEnabled) missingConnections.Add("Portal 비활성화");
            if (!portal.CanKuneKuneUse) missingConnections.Add("Portal의 Can Enter가 꺼짐");
            if (portal.TargetMap == null) missingConnections.Add("Portal Target Map 미지정");
            if (portal.TargetEntryPoint == null) missingConnections.Add("Portal Target Entry Point 미지정");
            if (portal.TransitionArea == null) missingConnections.Add("Portal Collider2D 없음");
            else if (!portal.TransitionArea.enabled) missingConnections.Add("Portal Collider2D 비활성화");
        }

        if (missingConnections.Count > 0)
        {
            Debug.LogError("[Cutscene_TeddyBear] " + string.Join(", ", missingConnections), this);
            return;
        }

        base.PlayCutscene();
    }

    private void OnDialogFinished(DialogData dialog)
    {
        if (activationDialog == null || dialog != activationDialog || teddy == null ||
            !IsCutsceneRunning || _sequenceRoutine != null)
            return;

        _teddyStartPosition = teddy.localPosition;
        // 컨트롤러의 기본 Run 상태가 흔들림 도중 재생되지 않게 합니다.
        teddy.GetComponent<Animator>().enabled = false;
        teddy.gameObject.SetActive(true);
        _sequenceRoutine = StartCoroutine(ShakeTeddy());
    }

    private IEnumerator ShakeTeddy()
    {
        float elapsed = 0f;
        while (teddy != null && elapsed < shakeDuration)
        {
            float offset = Mathf.Sin(elapsed * shakeFrequency * Mathf.PI * 2f) * shakeAmplitude;
            teddy.localPosition = _teddyStartPosition + Vector3.right * offset;
            yield return null;
            elapsed += Time.deltaTime;
        }

        if (teddy != null)
            teddy.localPosition = _teddyStartPosition;

        yield return PlayTeddyAnimation();
        if (IsCutsceneRunning)
            yield return RunPlayerToPortal();
        _sequenceRoutine = null;
    }

    private IEnumerator PlayTeddyAnimation()
    {
        if (teddy == null) yield break;

        Animator animator = teddy != null ? teddy.GetComponent<Animator>() : null;
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogError("[Cutscene_TeddyBear] Teddy Animator 연결을 확인해주세요.", this);
            CompleteCutscene();
            yield break;
        }

        animator.enabled = true;
        animator.speed = 1f;
        // 비활성 상태였던 Animator도 초기화한 뒤 첫 프레임부터 교체합니다.
        animator.Update(0f);
        animator.Play("Teddy", 0, 0f);
        animator.Update(0f);

        // 한 번 재생되는 변신 클립이 끝난 뒤에만 플레이어를 달리게 합니다.
        while (animator != null && animator.isActiveAndEnabled)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName("Teddy"))
            {
                Debug.LogError("[Cutscene_TeddyBear] Animator의 Teddy 상태를 확인해주세요.", this);
                CompleteCutscene();
                yield break;
            }
            if (state.normalizedTime >= 1f)
                break;
            yield return null;
        }
    }

    private IEnumerator RunPlayerToPortal()
    {
        _isPlayerRunning = true;
        _playerMovement.SetFacingDirection(portal.TransitionArea.bounds.center.x - _playerBody.position.x);
        _playerMovement.SetCutsceneMovementControlled(true);
        _playerAnimation.SetCutsceneSprinting(true);

        // 물리를 켜 둔 채 이동하여 Portal의 실제 OnTriggerEnter2D가 전환을 실행합니다.
        while (_isPlayerRunning && _playerBody != null && portal != null)
        {
            yield return new WaitForFixedUpdate();
            if (!_isPlayerRunning) yield break;
            if (_playerBody == null || portal == null || !portal.isActiveAndEnabled ||
                portal.TransitionArea == null || !portal.TransitionArea.enabled)
                break;

            Vector2 position = _playerBody.position;
            position.x = Mathf.MoveTowards(position.x, portal.TransitionArea.bounds.center.x,
                _playerMovement.SprintingSpeed * Time.fixedDeltaTime);
            _playerBody.MovePosition(position);
        }

        if (_isPlayerRunning)
        {
            StopPlayerRun();
            CompleteCutscene();
        }
    }

    private void OnPortalEntered(TeleportTrigger enteredPortal)
    {
        if (enteredPortal != portal || !_isPlayerRunning || !IsCutsceneRunning) return;

        StopPlayerRun();
        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = null;
        }
        director.Stop();
        CompleteCutscene();
    }

    private void StopPlayerRun()
    {
        if (!_isPlayerRunning) return;
        _isPlayerRunning = false;
        _playerMovement?.SetCutsceneMovementControlled(false);
        _playerAnimation?.SetCutsceneSprinting(false);
    }

    protected override IEnumerator CutsceneEndRoutine()
    {
        // Timeline 길이와 무관하게 실제 Portal 진입 시점까지 컷씬 잠금을 유지합니다.
        while (IsCutsceneRunning)
            yield return null;
    }

    protected override void OnDisable()
    {
        bool wasRunning = IsCutsceneRunning;
        if (_dialogController != null)
            _dialogController.DialogFinished -= OnDialogFinished;
        if (portal != null) portal.PlayerEntered -= OnPortalEntered;
        StopPlayerRun();

        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = null;
            if (teddy != null)
                teddy.localPosition = _teddyStartPosition;
        }

        base.OnDisable();
        if (wasRunning) CutsceneManager.Instance?.EndCutscene();
    }
}
