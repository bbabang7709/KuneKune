using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[System.Serializable]
public class LetterboxClip : PlayableAsset, ITimelineClipAsset
{
    [Header("Letterbox")]
    [Tooltip("레터박스가 등장하는 시간")]
    public float enterDuration = 0.5f;

    [Tooltip("레터박스가 사라지는 시간")]
    public float exitDuration = 0.5f;

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        ScriptPlayable<LetterboxBehaviour> playable = ScriptPlayable<LetterboxBehaviour>.Create(graph);

        LetterboxBehaviour behaviour = playable.GetBehaviour();

        behaviour.enterDuration = enterDuration;
        behaviour.exitDuration = exitDuration;

        return playable;   
    }
}
