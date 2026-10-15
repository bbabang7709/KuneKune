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

    [SerializeField] private DoorBangingEffect doorBanging;

    [Tooltip("눈 마주침 연출이 끝난 직후 이 시간(초) 동안은 추락 상호작용을 막는다 (연속 입력 방지)")]
    [SerializeField] private float interactCooldownAfterEncounter = 0.5f;

    private bool _playedEncounterCutscene = false;
    private bool _playedFallCutscene = false;
    private bool _isEncounterPlaying = false;
    private float _encounterFinishedTime = float.NegativeInfinity;

    public override void OnInteractPressed()
    {
        if (CutsceneManager.Instance.IsCutscenePlaying) return;
        if (_playedFallCutscene) return;

        // 눈 마주침 연출 중이거나 막 끝난 직후에는 구멍과 상호작용할 수 없다.
        if (_isEncounterPlaying) return;
        if (Time.unscaledTime - _encounterFinishedTime < interactCooldownAfterEncounter) return;

        if (!_playedEncounterCutscene)
        {
            _playedEncounterCutscene = true;
            StartCoroutine(EncounterRoutine());
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
        _isEncounterPlaying = true;

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
        if (doorBanging != null) doorBanging.StartBanging();


        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.RemoveMovementLock(this);
        }

        _isEncounterPlaying = false;
        _encounterFinishedTime = Time.unscaledTime;
        Debug.Log("쿠네쿠네 눈 마주침 컷씬 종료. 다시 상호작용해 아래로 떨어질 수 있습니다.");
    }
}
