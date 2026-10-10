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
        StopAllCoroutines();
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

    public void PlayCutscene()
    {
        if (!CanRunCutscene || _isEnding) return;
        if (playOnlyOnce && hasPlayed) return;
        hasPlayed = true;
        _isCutsceneRunning = true;

        CutsceneManager.Instance?.StartCutscene();
        StartCoroutine(CutsceneStartRoutine());
    }

    private IEnumerator CutsceneStartRoutine()
    {
        if (useFadeInOut && UIManager.Instance != null)
        {
            yield return new WaitForSeconds(0.2f);
            yield return UIManager.Instance.FadeOutGameplayUI(0.5f);
            yield return UIManager.Instance.FadeOut(0.5f);
        }

        if (!CanRunCutscene) yield break;
        OnCutsceneStartSetup();

        Debug.Log(
            $"<color=cyan>[DIRECTOR PLAY]</color> " +
            $"{GetType().Name} / {director.name} / frame={Time.frameCount}"
        );
        director.Play();

        yield return new WaitForSeconds(0.3f);

        if (!CanRunCutscene) yield break;
        if (useFadeInOut && UIManager.Instance != null)
        {
            yield return UIManager.Instance.FadeIn(0.5f);
        }
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

    private IEnumerator CutsceneEndRoutine()
    {

        if (useFadeInOut && UIManager.Instance != null)
        {
            yield return UIManager.Instance.FadeOut(0.5f);
        }

        if (!CanRunCutscene) yield break;
        OnCutsceneEndSetup();

        yield return new WaitForSeconds(1f);

        if (!CanRunCutscene) yield break;
        if (useFadeInOut && UIManager.Instance != null)
        {
            yield return UIManager.Instance.FadeIn(0.5f);
            if (!CanRunCutscene) yield break;
            yield return UIManager.Instance.FadeInGameplayUI(0.5f);
        }

        if (!CanRunCutscene) yield break;
        _isCutsceneRunning = false;
        _isEnding = false;
        CutsceneManager.Instance?.EndCutscene();
        OnCutsceneFinished();
    }

    protected virtual void OnCutsceneStartSetup() {}
    protected virtual void OnCutsceneEndSetup() {}
    // 화면과 게임 UI가 복구된 뒤 다음 게임 이벤트를 시작합니다.
    protected virtual void OnCutsceneFinished() {}
}
