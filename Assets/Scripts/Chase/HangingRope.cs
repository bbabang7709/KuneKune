using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class HangingRope : MonoBehaviour
{
    private const string RopeSfxID = "Rope";

    private bool _isCaughted = false;
    private Coroutine _deathRoutine;

    /// <summary>쿠네쿠네를 멈추고 암전한 뒤, 밧줄 효과음이 끝나면 사망(재시작) 처리를 합니다.</summary>
    private IEnumerator DieAfterSoundRoutine()
    {
        // 암전 중 쿠네쿠네가 플레이어를 덮치지 않도록 먼저 치웁니다.
        MapManager.Instance?.KuneKune?.Despawn();
        SoundManager.Instance?.SetBGMVolume(0f);

        if (UIManager.Instance != null)
            yield return UIManager.Instance.FadeOut(0.2f);

        float length = 0f;
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(RopeSfxID);
            length = SoundManager.Instance.GetSFXLength(RopeSfxID);
        }

        if (length > 0f)
            yield return new WaitForSecondsRealtime(length);

        _deathRoutine = null;
        SaveManager.Instance?.Load(true);
    }

    private void OnDisable()
    {
        // 대기 중 비활성화되면 코루틴이 끊겨 재시작이 누락되므로 즉시 처리합니다.
        if (_deathRoutine != null)
        {
            _deathRoutine = null;
            SaveManager.Instance?.Load(true);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_isCaughted) return;

        if (collision.CompareTag("Player"))
        {
            var playerMovement = collision.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                if (playerMovement.IsCrouching) return;
                else
                {
                    Debug.Log("밧줄에 걸려 사망했습니다.");
                    _isCaughted = true;

                    ControlLockManager.Instance?.AddInputLock(this);
                    ControlLockManager.Instance?.AddMovementLock(this);

                    _deathRoutine = StartCoroutine(DieAfterSoundRoutine());
                }
            }
        }
    }
}
