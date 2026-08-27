using UnityEngine;
using UnityEngine.Playables;

public class LetterboxBehaviour : PlayableBehaviour
{
    public float enterDuration = 0.5f;
    public float exitDuration = 0.5f;

    private bool hasStarted;
    private bool hasEnded;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (hasStarted) return;

        hasStarted = true;
        hasEnded = false;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowLetterbox(enterDuration);
        }
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if (!hasStarted || hasEnded) return;

        // Timeline이 정상적으로 Clip을 빠져나갔을 때 레터박스를 닫는다
        hasEnded = true;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideLetterbox(exitDuration);
        } 
    }

    public override void OnPlayableDestroy(Playable playabe)
    {
        hasStarted = false;
        hasEnded = false;
    }
}
