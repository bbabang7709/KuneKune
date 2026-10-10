using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Timeline 클립이 실행될 때 대화를 트리거하는 커스텀 PlayableBehaviour.
/// </summary>
public class DialogBehaviour : PlayableBehaviour
{
    /// <summary>
    /// 재생할 대화 데이터.
    /// </summary>
    public DialogData dialog;

    /// <summary>
    /// OnBehaviourPlay가 여러 번 호출되더라도
    /// 같은 Playable 인스턴스에서 대화가 중복 실행되지 않도록 방지합니다.
    /// </summary>
    private bool _started;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (_started) return;

        if (dialog == null) return;

        PlayableDirector director = playable.GetGraph().GetResolver() as PlayableDirector;
        // 대사 교체와 재생 알림은 현재 Timeline에 연결된 컨트롤러에서 처리합니다.
        DialogTimelineController controller = director != null
            ? director.GetComponent<DialogTimelineController>()
            : null;
        if (controller == null)
            controller = Object.FindFirstObjectByType<DialogTimelineController>();

        if (controller == null)
        {
            Debug.LogError(
                "[DialogBehaviour] DialogTimelineController를 찾을 수 없습니다."
            );

            return;
        }

        // 현재 Timeline을 재생하고 있는 PlayableDirector를 가져옵니다.
        //
        // 중요:
        // FindFirstObjectByType로 찾은 Controller가 가지고 있는
        // Director를 사용하면 다른 Timeline의 Director를 조작할 수 있습니다.
        //
        // 따라서 현재 PlayableGraph의 Resolver인 PlayableDirector를
        // 직접 가져와서 전달합니다.
        if (director == null)
        {
            Debug.LogError(
                $"[DialogBehaviour] 현재 Timeline의 PlayableDirector를 찾을 수 없습니다. " +
                $"Dialog={dialog.name}"
            );

            return;
        }

        _started = true;

        // 현재 Timeline의 Root 시간 + Dialog Playable의 Duration을
        // Dialog 종료 후 Resume할 시간으로 사용합니다.
        double clipEndTime = playable.GetGraph().GetRootPlayable(0).GetTime() + playable.GetDuration();

        // 현재 Dialog가 속해 있는 Timeline의 Director를 전달합니다.
        controller.PlayDialog(dialog, clipEndTime, director);
    }
}

