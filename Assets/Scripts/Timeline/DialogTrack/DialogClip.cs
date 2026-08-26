using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class DialogClip : PlayableAsset, ITimelineClipAsset
{
    [SerializeField]
    private DialogData dialog;

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(
        PlayableGraph graph,
        GameObject owner)
    {
        ScriptPlayable<DialogBehaviour> playable =
            ScriptPlayable<DialogBehaviour>.Create(graph);

        DialogBehaviour behaviour =
            playable.GetBehaviour();

        behaviour.dialog = dialog;

        return playable;
    }
}