using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Timeline과 DialogSystem 간의 재생 흐름을 제어하는 컨트롤러.
/// 대화 클립(Dialog Clip)이 실행될 때 타임라인을 일시 정지하고, 대화가 끝나면 이어서 재생합니다.
/// </summary>
[RequireComponent(typeof(PlayableDirector))]
public class DialogTimelineController : MonoBehaviour
{
    /// <summary>
    /// 타임라인을 재생하고 제어하는 PlayableDirector 컴포넌트.
    /// </summary>
    private PlayableDirector _director;

    /// <summary>
    /// 현재 대화가 진행 중이며 타임라인 재생 대기 상태인지 여부를 나타내는 플래그.
    /// 중복 실행을 방지하는 역할을 합니다.
    /// </summary>
    private bool _waitingForDialog;

    /// <summary>
    /// 대화가 끝난 후 타임라인이 다시 재생을 시작할 시간(Clip의 종료 시점).
    /// </summary>
    private double _resumeTime;

    /// <summary>
    /// 컴포넌트가 활성화될 때 호출되어 PlayableDirector 참조를 가져옵니다.
    /// </summary>
    private void Awake()
    {
        _director = GetComponent<PlayableDirector>();
    }

    /// <summary>
    /// Timeline의 DialogBehaviour에서 호출됩니다.
    /// 현재 재생 중인 대화 클립의 종료 시점을 기억하고, 대화 UI를 띄운 뒤 Timeline을 일시 정지합니다.
    /// </summary>
    /// <param name="dialog">출력할 대화 데이터.</param>
    /// <param name="clipEndTime">현재 재생 중인 클립이 끝나는 Timeline 상의 시간.</param>
    public void PlayDialog(DialogData dialog, double clipEndTime)
    {
        if (dialog == null) return;

        // 이미 대화가 진행 중이라면 중복 실행을 막습니다.
        if (_waitingForDialog) return;

        if (DialogSystem.Instance == null)
        {
            Debug.LogError("DialogSystem.Instance를 찾을 수 없습니다.");
            return;
        }

        // 대기 상태 진입 플래그를 켜고, 나중에 돌아갈 시간을 저장합니다.
        _waitingForDialog = true;
        _resumeTime = clipEndTime;

        // DialogSystem을 통해 실제 대화를 화면에 출력합니다.
        // 대화가 모두 끝나면 OnDialogFinished 콜백 메서드가 실행되도록 넘겨줍니다.
        DialogSystem.Instance.PlayDialog(dialog, OnDialogFinished);

        // 대화가 진행되는 동안 배경 연출 등이 지나가지 않도록 현재 Timeline을 멈춥니다.
        _director.Pause();
    }

    /// <summary>
    /// DialogSystem에서 플레이어가 대화를 모두 읽고 창을 닫았을 때(콜백) 호출됩니다.
    /// 타임라인을 일시 정지했던 시점(정확히는 클립의 끝)으로 이동시킨 후 다시 재생합니다.
    /// </summary>
    private void OnDialogFinished()
    {
        // 대기 상태가 아니었다면 무시합니다.
        if (!_waitingForDialog) return;

        // 대기 상태를 해제합니다.
        _waitingForDialog = false;

        // Director의 시간을 저장해둔 Dialogue Clip의 끝 지점으로 강제 이동시킵니다.
        // 이렇게 하면 대화 클립의 잔여 시간 동안 타임라인이 멈춰있는 현상을 방지할 수 있습니다.
        _director.time = _resumeTime;

        // 시간 이동 후 변경된 상태(애니메이션, 위치 등)를 화면에 즉시 반영(평가)합니다.
        _director.Evaluate();

        // 중단되었던 Timeline 재생을 다시 시작합니다.
        _director.Play();
    }
}