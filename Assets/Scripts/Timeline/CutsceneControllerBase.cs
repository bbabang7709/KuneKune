using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(PlayableDirector))]
public abstract class CutsceneControllerBase : MonoBehaviour
{
    protected PlayableDirector director;
    protected bool hasPlayed = false;

    [Header("Cutscene Settings")]
    [SerializeField] protected bool playOnlyOnce = true;
    [SerializeField] protected bool useFadeInOut = true;

    protected virtual void Awake()
    {
        director = GetComponent<PlayableDirector>();

        director.stopped += (PlayableDirector d) => EndCutscene();
    }

    public void PlayCutscene()
    {
        if (playOnlyOnce && hasPlayed) return;
        hasPlayed = true;

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

        OnCutsceneStartSetup();

        Debug.Log(
            $"<color=cyan>[DIRECTOR PLAY]</color> " +
            $"{GetType().Name} / {director.name} / frame={Time.frameCount}"
        );
        director.Play();

        yield return new WaitForSeconds(0.3f);

        if (useFadeInOut && UIManager.Instance != null)
        {
            yield return UIManager.Instance.FadeIn(0.5f);
        }
    }

    protected virtual void EndCutscene()
    {
        StartCoroutine(CutsceneEndRoutine());
    }

    private IEnumerator CutsceneEndRoutine()
    {

        if (useFadeInOut && UIManager.Instance != null)
        {
            yield return UIManager.Instance.FadeOut(0.5f);
        }

        OnCutsceneEndSetup();

        yield return new WaitForSeconds(1f);

        if (useFadeInOut && UIManager.Instance != null)
        {
            yield return UIManager.Instance.FadeIn(0.5f);
            yield return UIManager.Instance.FadeInGameplayUI(0.5f);
        }

        CutsceneManager.Instance?.EndCutscene();
        OnCutsceneFinished();
    }

    protected virtual void OnCutsceneStartSetup() {}
    protected virtual void OnCutsceneEndSetup() {}
    // 화면과 게임 UI가 복구된 뒤 다음 게임 이벤트를 시작합니다.
    protected virtual void OnCutsceneFinished() {}
}
