using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Timeline 창에서 트랙에 배치하여 사용할 수 있는 대화(Dialog) 클립 에셋.
/// 실행 시 DialogBehaviour를 생성하고 대화 데이터를 주입하는 역할을 합니다.
/// </summary>
public class DialogClip : PlayableAsset, ITimelineClipAsset
{
    /// <summary>
    /// 인스펙터(Inspector)에서 할당할 대화 데이터.
    /// 재생될 대사, 화자 정보 등이 담겨 있습니다.
    /// </summary>
    [SerializeField]
    private DialogData dialog;

    /// <summary>
    /// 이 클립이 타임라인에서 지원하는 기능(블렌딩, 루핑 등)을 정의합니다.
    /// 대화 클립은 기본적으로 블렌딩이나 반복 재생이 필요 없으므로 ClipCaps.None을 반환합니다.
    /// </summary>
    public ClipCaps clipCaps => ClipCaps.None;

    /// <summary>
    /// Timeline 그래프가 초기화되거나 실행될 때 호출되어, 
    /// 실제 로직을 수행할 Playable 인스턴스를 생성하고 반환합니다.
    /// </summary>
    /// <param name="graph">이 클립이 속한 PlayableGraph.</param>
    /// <param name="owner">Timeline을 재생하고 있는 게임 오브젝트.</param>
    /// <returns>생성 및 초기화된 ScriptPlayable 인스턴스.</returns>
    public override Playable CreatePlayable( PlayableGraph graph, GameObject owner)
    {
        // DialogBehaviour 로직을 실행할 수 있는 ScriptPlayable 객체를 생성합니다.
        ScriptPlayable<DialogBehaviour> playable = ScriptPlayable<DialogBehaviour>.Create(graph);

        // 생성된 Playable 인스턴스에서 내부 Behaviour 객체를 가져옵니다.
        DialogBehaviour behaviour = playable.GetBehaviour();

        // 인스펙터에서 설정한 대화(DialogData) 데이터를 Behaviour 객체에 전달(주입)합니다.
        behaviour.dialog = dialog;

        // 데이터 주입이 완료된 Playable 객체를 타임라인 시스템에 반환합니다.
        return playable;
    }
}