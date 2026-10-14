using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CutsceneTrigger : MonoBehaviour
{
    [SerializeField] private CutsceneControllerBase cutscene;
    private bool _isWaiting;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player") || _isWaiting || cutscene == null || cutscene.HasPlayed ||
            !cutscene.isActiveAndEnabled || (SaveManager.Instance != null && SaveManager.Instance.IsLoading))
            return;

        _isWaiting = true;
        ControlLockManager.Instance?.AddInputLock(this);
        ControlLockManager.Instance?.AddMovementLock(this);
        StartCoroutine(WaitAndPlayCutscene());
    }

    private IEnumerator WaitAndPlayCutscene()
    {
        // 회상 대사가 끝나기 전에 다음 컷씬의 대사가 실행되지 않도록 기다립니다.
        yield return new WaitUntil(() =>
            cutscene == null || !cutscene.isActiveAndEnabled ||
            ((SaveManager.Instance == null || !SaveManager.Instance.IsLoading) &&
             (CutsceneManager.Instance == null || !CutsceneManager.Instance.IsCutscenePlaying) &&
             (DialogSystem.Instance == null || !DialogSystem.Instance.IsDialogPlaying)));

        if (cutscene != null && cutscene.isActiveAndEnabled && !cutscene.HasPlayed)
            cutscene.PlayCutscene();

        ReleaseControls();
        _isWaiting = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _isWaiting = false;
        ReleaseControls();
    }

    private void ReleaseControls()
    {
        ControlLockManager.Instance?.RemoveInputLock(this);
        ControlLockManager.Instance?.RemoveMovementLock(this);
    }
}
