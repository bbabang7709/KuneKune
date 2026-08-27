using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class HouseCutsceneController : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;
    [SerializeField] private PlayableDirector cutscene;

    private bool hasPlayed = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayCutscene();
        }
    }

    private void PlayCutscene()
    {
        if (hasPlayed) return;

        hasPlayed = true;

        if (ControlLockManager.Instance != null)
            ControlLockManager.Instance.AddInputLock(this);

        StartCoroutine(CutsceneRoutine());
    }

    private IEnumerator CutsceneRoutine()
    {
        UIManager.Instance.FadeOutGameplayUI(0.5f);

        yield return UIManager.Instance?.FadeOut(0.5f);
        yield return UIManager.Instance?.FadeIn(0.5f);

        if (cutscene != null) cutscene.Play();
    }

    public void OnEndCutscene()
    {
        StartCoroutine(EndCutsceneRoutine());
    }

    private IEnumerator EndCutsceneRoutine()
    {
        yield return UIManager.Instance.FadeInGameplayUI(0.5f);

        if (ControlLockManager.Instance != null)
            ControlLockManager.Instance.RemoveInputLock(this);
        QuestManager.Instance.StartQuest("HouseCleanup");
    }
}
