using System.Collections;
using UnityEngine;

public class HouseEscapingSequence : MonoBehaviour
{
    [Header("Escpae Settings")]
    [Tooltip("플레이어에게 주어지는 도망 시간(초)")]
    [SerializeField] private float timeLimit = 20f;

    [Tooltip("이벤트 중 잠금 설정할 문")]
    [SerializeField] private Door[] _houseDoors;

    [SerializeField] private PlayerAppearance _player;

    private bool _isCaught = false;

    public void StartEscapeSequence()
    {
        StartCoroutine(EscpaeTimerRoutine());
    }

    private IEnumerator EscpaeTimerRoutine()
    {
        // TODO: BGM 재생
        SetDoorsLocked(true);

        EscapeTimerUI.Instance?.ShowTimer();
        float timeLeft = timeLimit;

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
        StartCoroutine(ChangeChpaterRoutine());
    }

    private IEnumerator ChangeChpaterRoutine()
    {
        yield return UIManager.Instance?.FadeOut(0.5f);
        
        MapManager.Instance?.InitMap(EMapType.School_Hallway, "school_hallway_0");
        if (_player != null) _player.SetAge(EPlayerAge.Student);

        yield return new WaitForSeconds(1f);

        yield return UIManager.Instance?.FadeIn(1.5f);

        SaveManager.Instance?.SaveChapterCheckpoint(EChapter.Chapter3_School);
        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.RemoveInputLock(this);
            ControlLockManager.Instance.RemoveMovementLock(this);
        }
    }

    private void SetDoorsLocked(bool isLocked)
    {
        foreach (Door door in _houseDoors)
        {
            if (door == null || door.LockDuringHouseChase == false)
                continue;
            door.SetHouseChaseLocked(isLocked);
        }
    }
}
