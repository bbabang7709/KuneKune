using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(PlayableDirector))]
public class DialogTimelineController : MonoBehaviour
{
    private PlayableDirector _director;

    private bool _waitingForDialog;
    private double _resumeTime;

    private void Awake()
    {
        _director = GetComponent<PlayableDirector>();
    }

    /// <summary>
    /// Timeline에서 호출된다.
    /// 현재 Dialogue Clip이 끝나는 시각을 기억하고 Timeline을 정지한다.
    /// </summary>
    public void PlayDialog(
        DialogData dialog,
        double clipEndTime)
    {
        if (dialog == null)
            return;

        if (_waitingForDialog)
            return;

        if (DialogSystem.Instance == null)
        {
            Debug.LogError(
                "DialogSystem.Instance를 찾을 수 없습니다."
            );

            return;
        }

        _waitingForDialog = true;
        _resumeTime = clipEndTime;

        DialogSystem.Instance.PlayDialog(
            dialog,
            OnDialogFinished
        );

        // 현재 Dialogue Clip에서 Timeline을 멈춘다.
        _director.Pause();
    }

    /// <summary>
    /// DialogSystem에서 대화가 완전히 끝났을 때 호출된다.
    /// </summary>
    private void OnDialogFinished()
    {
        if (!_waitingForDialog)
            return;

        _waitingForDialog = false;

        // Dialogue Clip의 끝으로 시간을 이동시킨다.
        _director.time = _resumeTime;

        // 해당 시간의 Timeline 상태를 즉시 평가한다.
        _director.Evaluate();

        // 다음 구간부터 계속 재생한다.
        _director.Play();
    }
}