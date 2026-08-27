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

    private Action _onDialogFinished;

    private Coroutine _activeDialogRoutine;

    /// <summary>
    /// 현재 대화가 진행 중인지 여부.
    /// </summary>
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

    /// <summary>
    /// 일반 대화를 시작한다.
    /// </summary>
    /// <param name="dialog">출력할 대화 데이터.</param>
    public void PlayDialog(DialogData dialog)
    {
        if (dialog == null || IsDialogPlaying) return;

        _activeDialogRoutine = StartCoroutine(PlayDialogRoutine(dialog));
    }

    public void PlayDialog(DialogData dialog, Action onFinished)
    {
        if (dialog == null || IsDialogPlaying) return;

        _onDialogFinished = onFinished;
        _activeDialogRoutine = StartCoroutine(PlayDialogRoutine(dialog));
    }

    /// <summary>
    /// 일반 대화의 전체 진행을 처리한다.
    /// </summary>
    /// <param name="dialog">출력할 대화 데이터.</param>
    /// <returns>코루틴 Enumerator.</returns>
    private IEnumerator PlayDialogRoutine(DialogData dialog)
    {
        IsDialogPlaying = true;
        yield return null;

        // 플레이어 이동을 정지한다.
        // if (playerMovement != null) playerMovement.SetMovementLocked(true);
        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.AddMovementLock(this);
            ControlLockManager.Instance.AddInputLock(this);
        }

        // 대화 UI를 표시한다.
        UIManager.Instance?.OpenDialog();

        foreach (string line in dialog.lines)
        {
            if (string.IsNullOrEmpty(line))
                continue;

            // 한 줄의 텍스트를 타이핑한다.
            yield return StartCoroutine(textSystem.TypeText(line));

            // 다음 대사를 기다린다.
            yield return WaitForAdvanceInput();
        }

        EndDialog();
    }

    /// <summary>
    /// 대화 진행 입력을 기다린다.
    /// </summary>
    private IEnumerator WaitForAdvanceInput()
    {
        yield return new WaitUntil(() => playerInput.IsDialogAdvancePressed);
        yield return WaitForInputRelease();
    }

    /// <summary>
    /// Interact 버튼이 해제될 때까지 기다린다.
    /// </summary>
    private IEnumerator WaitForInputRelease()
    {
        yield return new WaitUntil(() => !playerInput.IsDialogAdvanceHeld);
    }

    /// <summary>
    /// 현재 대화를 종료하고 플레이어 조작을 복구한다.
    /// </summary>
    public void EndDialog()
    {
        if (_activeDialogRoutine != null)
        {
            StopCoroutine(_activeDialogRoutine);
            _activeDialogRoutine = null;
        }

        UIManager.Instance?.CloseDialog();

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

    /// <summary>
    /// 이미지와 대화를 함께 사용하는 연출을 시작한다.
    /// 아이템뿐만 아니라 그림, 사진, 문서, 이벤트 이미지 등
    /// 다양한 상호작용에 사용할 수 있다.
    /// </summary>
    /// <param name="image">표시할 이미지.</param>
    /// <param name="dialog">출력할 대화 데이터.</param>
    /// <param name="onFinished">연출 종료 후 호출할 콜백.</param>
    public void PlayIllustrationDialog(Sprite image, DialogData dialog, System.Action onFinished)
    {
        if (IsDialogPlaying) return;
        _activeDialogRoutine = StartCoroutine(PlayIllustrationDialogRoutine(image, dialog, onFinished));
    }

    /// <summary>
    /// 이미지와 대화가 함께 표시되는 연출을 처리한다.
    /// </summary>
    /// <param name="image">표시할 이미지.</param>
    /// <param name="dialog">출력할 대화 데이터.</param>
    /// <param name="onFinished">연출 종료 후 호출할 콜백.</param>
    /// <returns>코루틴 Enumerator.</returns>
    private IEnumerator PlayIllustrationDialogRoutine(Sprite image, DialogData dialog, System.Action onFinished)
    {
        IsDialogPlaying = true;
        yield return null;

        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.AddMovementLock(this);
            ControlLockManager.Instance.AddInputLock(this);
        }

        // 이미지가 있다면 표시한다.
        UIManager.Instance.OpenIllustration(image);

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
            // 대화가 없다면 Interact 입력을 기다린다.
            yield return WaitForAdvanceInput();
        }

        UIManager.Instance?.CloseIllustration();
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