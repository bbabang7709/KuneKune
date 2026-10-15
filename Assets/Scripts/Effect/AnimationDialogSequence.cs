using System.Collections;
using UnityEngine;

/// <summary>
/// 애니메이션을 재생하고, 끝날 때까지 기다린 뒤 다이얼로그를 출력하는 연출.
/// UnityEvent, 트리거, 다른 스크립트에서 Play()를 호출해 사용한다.
/// </summary>
public class AnimationDialogSequence : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [Tooltip("재생을 시작할 Trigger 파라미터 이름 (비워두면 Trigger 없이 State만 직접 재생)")]
    [SerializeField] private string triggerName;
    [Tooltip("끝날 때까지 기다릴 Animator State 이름")]
    [SerializeField] private string stateName;
    [SerializeField] private int layer = 0;

    [Header("Dialog")]
    [SerializeField] private DialogData dialog;

    [Header("Options")]
    [Tooltip("애니메이션이 재생되는 동안 이동/입력을 잠근다")]
    [SerializeField] private bool lockInputWhilePlaying = true;
    [Tooltip("State가 끝나지 않거나 진입하지 못할 때 무한 대기를 막는 최대 대기 시간(초)")]
    [SerializeField] private float maxWaitSeconds = 10f;
    [SerializeField] private bool playOnce = true;

    private bool _isPlaying;
    private bool _hasPlayed;
    private bool _isLocked;

    /// <summary>애니메이션이 끝나면 이 컴포넌트에 지정한 dialog를 출력한다.</summary>
    public void Play()
    {
        PlayAnimation(() =>
        {
            if (dialog != null)
                DialogSystem.Instance?.PlayDialog(dialog);
        });
    }

    /// <summary>
    /// 애니메이션만 재생하고, 끝나면 onFinished를 호출한다. (DialogTrigger 등에서 다이얼로그를 직접 제어할 때 사용)
    /// </summary>
    public void PlayAnimation(System.Action onFinished)
    {
        if (_isPlaying || (playOnce && _hasPlayed)) return;

        StartCoroutine(PlayRoutine(onFinished));
    }

    private void OnDisable()
    {
        _isPlaying = false;
        ReleaseLock();
    }

    private IEnumerator PlayRoutine(System.Action onFinished)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
        {
            Debug.LogWarning($"[{nameof(AnimationDialogSequence)}] Animator 또는 State 이름이 비어 있습니다.", this);
            // 연출이 불가능해도 후속 다이얼로그는 막히지 않게 한다.
            onFinished?.Invoke();
            yield break;
        }

        _isPlaying = true;
        _hasPlayed = true;
        AcquireLock();

        if (!string.IsNullOrEmpty(triggerName))
            animator.SetTrigger(triggerName);
        else
            animator.Play(stateName, layer, 0f);

        // 1. 목표 State에 진입할 때까지 대기 (Trigger는 다음 프레임 이후에 전이된다)
        float deadline = Time.unscaledTime + maxWaitSeconds;
        yield return null;

        while (!IsInState() || animator.IsInTransition(layer))
        {
            if (Time.unscaledTime > deadline)
            {
                Debug.LogWarning($"[{nameof(AnimationDialogSequence)}] '{stateName}' State에 진입하지 못했습니다.", this);
                break;
            }
            yield return null;
        }

        // 2. 애니메이션이 끝날 때까지 대기 (루프 애니메이션이면 maxWaitSeconds 후 넘어간다)
        while (IsInState() && animator.GetCurrentAnimatorStateInfo(layer).normalizedTime < 1f)
        {
            if (Time.unscaledTime > deadline) break;
            yield return null;
        }

        ReleaseLock();
        _isPlaying = false;

        // 3. 후속 처리 (다이얼로그 출력 등. DialogSystem이 재생 중 입력을 스스로 잠근다)
        onFinished?.Invoke();
    }

    private bool IsInState()
    {
        return animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName);
    }

    private void AcquireLock()
    {
        if (!lockInputWhilePlaying || _isLocked || ControlLockManager.Instance == null) return;

        ControlLockManager.Instance.AddInputLock(this);
        ControlLockManager.Instance.AddMovementLock(this);
        _isLocked = true;
    }

    private void ReleaseLock()
    {
        if (!_isLocked) return;

        _isLocked = false;
        if (ControlLockManager.Instance == null) return;

        ControlLockManager.Instance.RemoveInputLock(this);
        ControlLockManager.Instance.RemoveMovementLock(this);
    }
}
