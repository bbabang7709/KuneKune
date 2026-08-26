using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Timeline 클립이 실행될 때 대화를 트리거하는 커스텀 PlayableBehaviour.
/// 타임라인의 재생 헤드가 해당 클립 구간에 진입할 때 작동한다.
/// </summary>
public class DialogBehaviour : PlayableBehaviour
{
    /// <summary>
    /// 재생할 대화 데이터. (주로 ScriptableObject 형태)
    /// Timeline 에디터의 인스펙터 창에서 할당된다.
    /// </summary>
    public DialogData dialog;

    /// <summary>
    /// 클립 실행 중 대화가 이미 시작되었는지 여부를 추적하여 
    /// OnBehaviourPlay가 여러 번 호출되더라도 중복 실행되지 않도록 방지한다.
    /// </summary>
    private bool _started;

    /// <summary>
    /// Timeline의 재생 헤드가 이 클립의 시작 시간에 도달하여 클립이 재생될 때 호출된다.
    /// 씬 내의 대화 컨트롤러를 찾아 대화 UI를 띄우도록 명령한다.
    /// </summary>
    /// <param name="playable">현재 실행 중인 Playable 객체.</param>
    /// <param name="info">현재 프레임의 재생 상태 정보.</param>
    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        // 이미 대화가 시작되었거나, 재생할 대화 데이터가 없다면 무시한다.
        if (_started)
            return;

        if (dialog == null)
            return;

        // 씬에 존재하는 DialogTimelineController를 찾는다.
        DialogTimelineController controller =
            Object.FindFirstObjectByType<DialogTimelineController>();

        if (controller == null)
        {
            Debug.LogError("DialogueTimelineController를 찾을 수 없습니다.");
            return;
        }

        _started = true;

        // 현재 Timeline 위치 + 이 Clip의 길이 = Clip 종료 시점
        // Timeline 루트의 현재 시간을 기준으로 이 클립이 끝나는 절대적인 타임라인 시간을 계산한다.
        double clipEndTime =
            playable.GetGraph().GetRootPlayable(0).GetTime()
            + playable.GetDuration();

        // 계산된 클립 종료 시점과 함께 대화 재생을 요청한다.
        // 컨트롤러는 이 종료 시점이 되면 대화창을 닫거나 다음 처리를 진행할 수 있다.
        controller.PlayDialog(dialog, clipEndTime);
    }
}