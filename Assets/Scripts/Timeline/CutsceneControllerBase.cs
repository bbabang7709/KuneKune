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
            yield return new WaitForSeconds(0.5f);
            yield return UIManager.Instance.FadeOutGameplayUI(0.5f);
        }

        OnCutsceneStartSetup();
        director.Play();
    }

    protected virtual void EndCutscene()
    {
        StartCoroutine(CutsceneEndRoutine());
    }

    private IEnumerator CutsceneEndRoutine()
    {
        if (useFadeInOut && UIManager.Instance != null)
        {
            yield return UIManager.Instance.FadeInGameplayUI(0.5f);
        }

        OnCutsceneEndSetup();

        CutsceneManager.Instance?.EndCutscene();
    }

    protected virtual void OnCutsceneStartSetup() {}
    protected virtual void OnCutsceneEndSetup() {}
}
