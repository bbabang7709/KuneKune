using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 게임 내 대화의 전체 흐름을 관리한다.
/// 대화 시작 및 종료, 플레이어 입력 잠금,
/// 대화 텍스트 출력과 대화 진행을 담당한다.
/// </summary>
public class DialogSystem : MonoBehaviour
{
    public static DialogSystem Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerInputHandler playerInput;
    [SerializeField] private DialogTextSystem textSystem;
    
    // 💡 변경점 1: 불필요해진 playerMovement 참조 제거 (ControlLockManager가 대신함)

    private Action _onDialogFinished;
    
    // 💡 변경점 2: 진행 중인 코루틴을 정확히 추적하기 위한 변수
    private Coroutine _activeDialogRoutine;

    public bool IsDialogPlaying { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void PlayDialog(DialogData dialog)
    {
        if (dialog == null || IsDialogPlaying) return;
        
        // 💡 코루틴 참조 저장
        _activeDialogRoutine = StartCoroutine(PlayDialogRoutine(dialog));
    }

    public void PlayDialog(DialogData dialog, Action onFinished)
    {
        if (dialog == null || IsDialogPlaying) return;

        _onDialogFinished = onFinished;
        _activeDialogRoutine = StartCoroutine(PlayDialogRoutine(dialog));
    }

    private IEnumerator PlayDialogRoutine(DialogData dialog)
    {
        IsDialogPlaying = true;

        // 💡 변경점 3: 이동과 입력을 명확하게 모두 잠금
        // 한 프레임 양보하기 전에 잠가야 그 사이에 이동이나 상호작용이 새지 않는다.
        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.AddMovementLock(this);
            ControlLockManager.Instance.AddInputLock(this);
        }

        yield return null;

        UIManager.Instance?.OpenDialog();

        foreach (string line in dialog.lines)
        {
            if (string.IsNullOrEmpty(line)) continue;

            yield return StartCoroutine(textSystem.TypeText(line));
            yield return WaitForAdvanceInput();
        }

        EndDialog();
    }

    private IEnumerator WaitForAdvanceInput()
    {
        yield return new WaitUntil(() => playerInput.IsDialogAdvancePressed);
        yield return WaitForInputRelease();
    }

    private IEnumerator WaitForInputRelease()
    {
        yield return new WaitUntil(() => !playerInput.IsDialogAdvanceHeld);
    }

    public void EndDialog()
    {
        // 💡 변경점 4: 모든 코루틴을 끄는 대신, 실행 중인 대화 코루틴만 안전하게 종료
        if (_activeDialogRoutine != null)
        {
            StopCoroutine(_activeDialogRoutine);
            _activeDialogRoutine = null;
        }

        UIManager.Instance?.CloseDialog();

        // 잠금 해제
        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.RemoveMovementLock(this);
            ControlLockManager.Instance.RemoveInputLock(this);
        }

        IsDialogPlaying = false;

        Action callback = _onDialogFinished;
        _onDialogFinished = null;
        callback?.Invoke();
    }

    public void PlayFullScreenImageDialog(Sprite image, DialogData dialog, System.Action onFinished)
    {
        if (IsDialogPlaying) return;
        _activeDialogRoutine = StartCoroutine(PlayFullScreenImageDialogRoutine(image, dialog, onFinished));
    }

    private IEnumerator PlayFullScreenImageDialogRoutine(Sprite image, DialogData dialog, Action onFinished)
    {
        IsDialogPlaying = true;

        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.AddMovementLock(this);
            ControlLockManager.Instance.AddInputLock(this);
        }

        yield return null;

        UIManager.Instance.OpenFullScreenImage(image);

        if (dialog != null && dialog.lines != null && dialog.lines.Length > 0)
        {
            UIManager.Instance?.OpenDialog();

            foreach (string line in dialog.lines)
            {
                if (string.IsNullOrEmpty(line)) continue;

                yield return StartCoroutine(textSystem.TypeText(line));
                yield return WaitForAdvanceInput();
            }
        }
        else
        {
            yield return WaitForAdvanceInput();
        }

        UIManager.Instance?.CloseFullScreenImage();
        UIManager.Instance?.CloseDialog();

        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.RemoveMovementLock(this);
            ControlLockManager.Instance.RemoveInputLock(this);
        }

        IsDialogPlaying = false;
        onFinished?.Invoke();
    }

    public void PlayItemDialog(Sprite image, DialogData dialog, System.Action onFinished)
    {
        if (IsDialogPlaying) return;
        _activeDialogRoutine = StartCoroutine(PlayItemDialogRoutine(image, dialog, onFinished));
    }

    private IEnumerator PlayItemDialogRoutine(Sprite image, DialogData dialog, Action onFinished)
    {
        IsDialogPlaying = true;

        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.AddMovementLock(this);
            ControlLockManager.Instance.AddInputLock(this);
        }

        yield return null;

        UIManager.Instance.OpenItemImage(image);

        if (dialog != null && dialog.lines != null && dialog.lines.Length > 0)
        {
            UIManager.Instance?.OpenDialog();

            foreach (string line in dialog.lines)
            {
                if (string.IsNullOrEmpty(line)) continue;

                yield return StartCoroutine(textSystem.TypeText(line));
                yield return WaitForAdvanceInput();
            }
        }
        else
        {
            yield return WaitForAdvanceInput();
        }

        UIManager.Instance?.CloseItemImage();
        UIManager.Instance?.CloseDialog();

        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.RemoveMovementLock(this);
            ControlLockManager.Instance.RemoveInputLock(this);
        }

        IsDialogPlaying = false;
        onFinished?.Invoke();
    }
}