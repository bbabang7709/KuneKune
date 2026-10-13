using System;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Timeline과 DialogSystem 간의 재생 흐름을 제어하는 컨트롤러.
///
/// Dialog 클립이 실행되면 해당 Dialog가 속해 있는 Timeline의
/// PlayableDirector를 일시 정지하고,
/// Dialog가 끝나면 같은 Timeline을 이어서 재생합니다.
/// </summary>
public class DialogTimelineController : MonoBehaviour
{
    public event Action<DialogData> DialogStarted;
    public event Action<DialogData> DialogFinished;

    // Timeline에서 교체 대상으로 지정한 원본 대사 (예: Toilet_0).
    private DialogData _overrideSource;
    // 원본 대사 대신 이번에 재생할 대사 (예: Toilet_1 또는 Toilet_2).
    private DialogData _overrideDialog;

    public void SetDialogOverride(DialogData originalDialog, DialogData replacementDialog)
    {
        _overrideSource = originalDialog;
        _overrideDialog = replacementDialog;
    }

    public void ClearDialogOverride()
    {
        _overrideSource = null;
        _overrideDialog = null;
    }

    /// <summary>
    /// 현재 대화가 진행 중인지 여부.
    /// </summary>
    private bool _waitingForDialog;
    private DialogData _currentDialog;

    /// <summary>
    /// 대화가 끝난 후 Timeline을 재개할 시간.
    /// </summary>
    private double _resumeTime;

    /// <summary>
    /// 현재 대화가 실행되고 있는 Timeline의 Director.
    ///
    /// 중요:
    /// 이 Director를 Awake에서 GetComponent로 가져오지 않습니다.
    /// DialogBehaviour에서 현재 실행 중인 Timeline의 Director를
    /// 직접 전달받습니다.
    /// </summary>
    private PlayableDirector _currentDirector;

    /// <summary>
    /// Timeline의 DialogBehaviour에서 호출됩니다.
    /// 현재 실행 중인 Dialog를 표시하고,
    /// 해당 Dialog가 속한 Timeline을 일시 정지합니다.
    /// </summary>
    /// <param name="dialog">
    /// 출력할 대화 데이터
    /// </param>
    /// <param name="clipEndTime">
    /// Dialog Clip 종료 시점
    /// </param>
    /// <param name="director">
    /// 현재 Dialog가 실행되고 있는 Timeline의 PlayableDirector
    /// </param>
    public void PlayDialog(DialogData dialog, double clipEndTime, PlayableDirector director)
    {
        if (dialog == null) return;
        if (director == null) return;
        if (_waitingForDialog) return;

        if (DialogSystem.Instance == null)
        {
            Debug.LogError(
                "[DialogTimelineController] DialogSystem.Instance를 찾을 수 없습니다."
            );

            return;
        }

        if (DialogSystem.Instance.IsDialogPlaying)
            return;

        if (dialog == _overrideSource && _overrideDialog != null)
            dialog = _overrideDialog;

        _waitingForDialog = true;
        _currentDialog = dialog;

        _resumeTime = clipEndTime;
        _currentDirector = director;

        DialogSystem.Instance.PlayDialog(dialog, OnDialogFinished);
        DialogStarted?.Invoke(dialog);

        _currentDirector.playableGraph.GetRootPlayable(0).SetSpeed(0d);
    }

    /// <summary>
    /// DialogSystem에서 대화가 모두 끝났을 때 호출됩니다.
    /// </summary>
    private void OnDialogFinished()
    {
        if (!_waitingForDialog) return;
        _waitingForDialog = false;

        DialogData finishedDialog = _currentDialog;
        _currentDialog = null;

        if (_currentDirector != null && _currentDirector.playableGraph.IsValid())
            _currentDirector.playableGraph.GetRootPlayable(0).SetSpeed(1d);
        _currentDirector = null;

        DialogFinished?.Invoke(finishedDialog);
    }
}
