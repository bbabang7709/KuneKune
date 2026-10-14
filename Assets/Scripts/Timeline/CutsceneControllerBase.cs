using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(PlayableDirector))]
public abstract class CutsceneControllerBase : MonoBehaviour
{
    protected PlayableDirector director;
    protected bool hasPlayed = false;
    public bool HasPlayed => hasPlayed;

    [Header("Cutscene Settings")]
    [SerializeField] protected bool playOnlyOnce = true;
    [SerializeField] protected bool useFadeInOut = true;

    private bool _isCutsceneRunning;
    private bool _endRequested;
    private bool _isEnding;
    private bool _isHoldingControls;
    private bool _isFadedOut;

    protected bool IsCutsceneRunning => _isCutsceneRunning;

    private bool CanRunCutscene => gameObject.activeInHierarchy && isActiveAndEnabled &&
        gameObject.scene.isLoaded && (SaveManager.Instance == null || !SaveManager.Instance.IsLoading);

    protected virtual void Awake()
    {
        director = GetComponent<PlayableDirector>();
    }

    protected virtual void OnEnable()
    {
        director.played += OnDirectorPlayed;
        director.stopped += OnDirectorStopped;
        _isCutsceneRunning = director.state == PlayState.Playing;
    }

    protected virtual void OnDisable()
    {
        if (director != null)
        {
            director.played -= OnDirectorPlayed;
            director.stopped -= OnDirectorStopped;
        }

        _isCutsceneRunning = false;
        _endRequested = false;
        _isEnding = false;
        ReleaseControls();
        StopAllCoroutines();

        // 암전 중에 오브젝트가 꺼져 코루틴이 끊기면 화면이 계속 검게 남으므로 복구합니다.
        if (_isFadedOut)
        {
            _isFadedOut = false;
            UIManager.Instance?.EnsureFadedIn();
        }
    }

    /// <summary>
    /// 트리거로 컷씬을 기다리는 동안 PlayCutscene 호출 전의 프레임 틈에 조작이 새지 않도록 잠급니다.
    /// PlayCutscene이 컷씬을 시작하면 CutsceneManager의 잠금으로 넘어가며 자동으로 해제됩니다.
    /// 세이브 로드가 잠금을 모두 지울 수 있으므로 대기 후에 다시 호출해도 됩니다.
    /// </summary>
    protected void HoldControls()
    {
        if (ControlLockManager.Instance == null) return;

        ControlLockManager.Instance.AddInputLock(this);
        ControlLockManager.Instance.AddMovementLock(this);
        _isHoldingControls = true;
    }

    protected void ReleaseControls()
    {
        if (!_isHoldingControls) return;
        _isHoldingControls = false;

        if (ControlLockManager.Instance == null) return;

        ControlLockManager.Instance.RemoveInputLock(this);
        ControlLockManager.Instance.RemoveMovementLock(this);
    }

    private void OnDirectorPlayed(PlayableDirector playedDirector)
    {
        _isCutsceneRunning = true;
        _endRequested = false;
    }

    private void OnDirectorStopped(PlayableDirector stoppedDirector)
    {
        EndCutscene();
    }

    public virtual void PlayCutscene()
    {
        if (!CanRunCutscene || _isEnding || (playOnlyOnce && hasPlayed))
        {
            ReleaseControls();
            return;
        }

        hasPlayed = true;
        _isCutsceneRunning = true;

        CutsceneManager.Instance?.StartCutscene();
        // 같은 프레임에 CutsceneManager의 잠금으로 넘어가므로 대기용 잠금은 틈 없이 해제됩니다.
        ReleaseControls();
        StartCoroutine(CutsceneStartRoutine());
    }

    private IEnumerator CutsceneStartRoutine()
    {
        if (useFadeInOut && UIManager.Instance != null)
        {
            yield return new WaitForSeconds(0.2f);
            yield return UIManager.Instance.FadeOutGameplayUI(0.5f);
            _isFadedOut = true;
            yield return UIManager.Instance.FadeOut(0.5f);
        }

        if (!CanRunCutscene) yield break;
        OnCutsceneStartSetup();

        Debug.Log(
            $"<color=cyan>[DIRECTOR PLAY]</color> " +
            $"{GetType().Name} / {director.name} / frame={Time.frameCount}"
        );
        director.Play();

        yield return WaitBeforeCutsceneReveal();

        if (!CanRunCutscene || !_isCutsceneRunning || _isEnding) yield break;
        if (useFadeInOut && UIManager.Instance != null)
        {
            yield return UIManager.Instance.FadeIn(0.5f);
            _isFadedOut = false;
        }
    }

    // 기본 컷씬은 짧게 기다리고, 대사를 암전 중에 보여주는 컷씬은 종료 이벤트까지 기다립니다.
    protected virtual IEnumerator WaitBeforeCutsceneReveal()
    {
        yield return new WaitForSeconds(0.3f);
    }

    protected virtual void EndCutscene()
    {
        // Director의 stopped는 씬 종료 중에도 호출됩니다. 이 콜백 안에서는 코루틴을 시작하지 않습니다.
        if (!CanRunCutscene || !_isCutsceneRunning || _isEnding) return;
        _endRequested = true;
    }

    private void Update()
    {
        if (!_endRequested) return;
        _endRequested = false;
        if (!CanRunCutscene || !_isCutsceneRunning || _isEnding) return;

        _isEnding = true;
        StartCoroutine(CutsceneEndRoutine());
    }

    protected virtual IEnumerator CutsceneEndRoutine()
    {

        if (useFadeInOut && UIManager.Instance != null)
        {
            _isFadedOut = true;
            yield return UIManager.Instance.FadeOut(0.5f);
        }

        if (!CanRunCutscene) yield break;
        OnCutsceneEndSetup();

        yield return new WaitForSeconds(1f);

        if (!CanRunCutscene) yield break;
        if (useFadeInOut && UIManager.Instance != null)
        {
            yield return UIManager.Instance.FadeIn(0.5f);
            _isFadedOut = false;
            if (!CanRunCutscene) yield break;
            yield return UIManager.Instance.FadeInGameplayUI(0.5f);
        }

        if (!CanRunCutscene) yield break;
        CompleteCutscene();
    }

    protected void CompleteCutscene()
    {
        if (!CanRunCutscene || !_isCutsceneRunning) return;

        _isCutsceneRunning = false;
        _isEnding = false;
        _endRequested = false;
        CutsceneManager.Instance?.EndCutscene();
        OnCutsceneFinished();
    }

    protected virtual void OnCutsceneStartSetup() {}
    protected virtual void OnCutsceneEndSetup() {}
    // 화면과 게임 UI가 복구된 뒤 다음 게임 이벤트를 시작합니다.
    protected virtual void OnCutsceneFinished() {}
}
