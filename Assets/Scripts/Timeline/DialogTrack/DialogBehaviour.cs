using UnityEngine;
using UnityEngine.Playables;

public class DialogBehaviour : PlayableBehaviour
{
    public DialogData dialog;

    private bool _started;

    public override void OnBehaviourPlay(
        Playable playable,
        FrameData info)
    {
        if (_started)
            return;

        if (dialog == null)
            return;

        DialogTimelineController controller =
            Object.FindFirstObjectByType<DialogTimelineController>();

        if (controller == null)
        {
            Debug.LogError(
                "DialogueTimelineController를 찾을 수 없습니다."
            );

            return;
        }

        _started = true;

        // 현재 Timeline 위치 + 이 Clip의 길이 = Clip 종료 시점
        double clipEndTime =
            playable.GetGraph().GetRootPlayable(0).GetTime()
            + playable.GetDuration();

        controller.PlayDialog(
            dialog,
            clipEndTime
        );
    }
}