using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class BrokenFloor : InteractableBase
{
    [SerializeField] private GameObject encounterSceneRoot;
    [SerializeField] private Animator imageAnim;
    [SerializeField] private string triggerName = "Encounter";
    [SerializeField] private float animationDuration;

    [Tooltip("다이얼로그 순서대로 넣기")]
    [SerializeField] private DialogData[] dialogs;
    [SerializeField] private bool[] needToStop;

    [SerializeField] private FallCutsceneController fallCutscene;

    [SerializeField] private Collider2D triggerCollider;

    private bool _playedEncounterCutscene = false;
    private bool _playedFallCutscene = false;

    public override void OnInteractPressed()
    {
        if (CutsceneManager.Instance.IsCutscenePlaying) return;
        if (_playedFallCutscene) return;

        if (!_playedEncounterCutscene)
        {
            StartCoroutine(EncounterRoutine());
            _playedEncounterCutscene = true;
            Debug.Log("쿠네쿠네 눈 마주침 컷씬 종료. 다시 상호작용해 아래로 떨어질 수 있습니다.");
        }
        else
        {
            Debug.Log("추락 컷씬 재생.");
            if (fallCutscene != null) fallCutscene.PlayCutscene();   
            _playedFallCutscene = true;
            triggerCollider.enabled = false;
        }
    }

    private IEnumerator EncounterRoutine()
    {
        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.AddMovementLock(this);
        }
        if (encounterSceneRoot != null) encounterSceneRoot.SetActive(true);

        for (int i = 0; i < dialogs.Length; i++)
        {
            if (DialogSystem.Instance != null && dialogs[i] != null)
            {
                DialogSystem.Instance.PlayDialog(dialogs[i]);
                yield return new WaitUntil(() => DialogSystem.Instance.IsDialogPlaying == false);
                if (needToStop[i])
                {
                    if (imageAnim != null) imageAnim.SetTrigger(triggerName);
                    yield return new WaitForSeconds(animationDuration + 1f);
                }
            }
        }

        yield return new WaitForSeconds(2f);
        if (encounterSceneRoot != null) encounterSceneRoot.SetActive(false);

        // TODO: 문 쾅쾅쾅!! 연출 넣기

        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.RemoveMovementLock(this);
        }
    }
}
