using UnityEngine;
using UnityEngine.Events;

public class HangingRope : MonoBehaviour
{
    private bool _isCaughted = false;

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

                    // TODO: 효과음 재생

                    if (SaveManager.Instance != null) SaveManager.Instance.Load(true);
                }
            }
        }
    }
}
