using System.Collections;
using UnityEngine;

public class HouseEscapingSequence : MonoBehaviour
{
    [Header("Escape Settings")]
    [Tooltip("플레이어에게 주어지는 도망 시간(초)")]
    [SerializeField] private float timeLimit = 20f;

    [Tooltip("이벤트 중 잠금 설정할 문")]
    [SerializeField] private Door[] _houseDoors;

    [SerializeField] private PlayerAppearance _player;

    [SerializeField] private DialogData caughtDialog;
    [SerializeField] private DialogData firstVisitSchoolDialog;

    private bool _isCaught = false;
    private bool _isRunning;

    public void StartEscapeSequence()
    {
        if (_isRunning || _isCaught) return;

        _isRunning = true;
        StartCoroutine(EscapeTimerRoutine());
    }

    private IEnumerator EscapeTimerRoutine()
    {
        // TODO: BGM 재생
        SetDoorsLocked(true);

        EscapeTimerUI.Instance?.ShowTimer();
        float timeLeft = timeLimit;
        EscapeTimerUI.Instance?.UpdateTime(timeLeft);

        while (timeLeft > 0)
        {
            if (_isCaught) yield break;

            timeLeft -= Time.deltaTime;

            EscapeTimerUI.Instance?.UpdateTime(Mathf.Max(0, timeLeft));

            if (timeLeft <= 5f)
            {
                // 긴박한 효과음
            }

            yield return null;
        }

        EscapeTimerUI.Instance?.HideTimer();

        if (!_isCaught) CatchPlayer();
    }

    public void CatchPlayer()
    {
        if (_isCaught) return;
        _isCaught = true;

        CaughtRoutine();
    }

    private void CaughtRoutine()
    {
        Debug.Log("쿠네쿠네에게 발각!!");

        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.AddInputLock(this);
            ControlLockManager.Instance.AddMovementLock(this);
        }

        SetDoorsLocked(false);
        EscapeTimerUI.Instance?.HideTimer();
        if (HouseChase.Instance != null)
            HouseChase.Instance.StopChase();
        else
            MapManager.Instance?.KuneKune?.Despawn();

        StartCoroutine(ChangeChapterRoutine());
    }

    private IEnumerator ChangeChapterRoutine()
    {
        yield return UIManager.Instance?.FadeOutKeepingDialog(0.5f);
        yield return PlayDialogAndWait(caughtDialog);
        
        MapManager.Instance?.InitMap(EMapType.School_Hallway, "school_hallway_0");
        if (_player == null && MapManager.Instance?.Player != null)
            _player = MapManager.Instance.Player.GetComponent<PlayerAppearance>();
        if (_player != null) _player.SetAge(EPlayerAge.Student);

        yield return UIManager.Instance?.FadeIn(1.5f);

        yield return PlayDialogAndWait(firstVisitSchoolDialog);

        SaveManager.Instance?.SaveChapterCheckpoint(EChapter.Chapter3_School);
        _isRunning = false;
        ReleaseControls();
    }

    private IEnumerator PlayDialogAndWait(DialogData dialog)
    {
        if (DialogSystem.Instance == null || dialog == null) yield break;

        // 이미 재생 중인 대사 때문에 이벤트 대사가 무시되지 않게 기다립니다.
        yield return new WaitUntil(() => DialogSystem.Instance == null || !DialogSystem.Instance.IsDialogPlaying);
        if (DialogSystem.Instance == null) yield break;

        DialogSystem.Instance.PlayDialog(dialog);
        yield return new WaitUntil(() => DialogSystem.Instance == null || !DialogSystem.Instance.IsDialogPlaying);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (_isRunning)
        {
            EscapeTimerUI.Instance?.HideTimer();
            UIManager.Instance?.RestoreDialogFadeOrder();
        }
        _isRunning = false;
        _isCaught = false;
        ReleaseControls();
    }

    private void ReleaseControls()
    {
        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.RemoveInputLock(this);
            ControlLockManager.Instance.RemoveMovementLock(this);
        }
    }

    private void SetDoorsLocked(bool isLocked)
    {
        if (_houseDoors == null) return;

        foreach (Door door in _houseDoors)
        {
            if (door == null || door.LockDuringChase == false)
                continue;
            door.SetHouseChaseLocked(isLocked);
        }
    }
}
