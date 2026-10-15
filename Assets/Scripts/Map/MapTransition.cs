using UnityEngine;

/// <summary>
/// 쿠네쿠네가 맵 사이의 최단 경로를 계산할 때 사용하는 문 또는 출구 정보입니다.
/// </summary>
public interface IKuneKuneMapRoute
{
    MapArea SourceMap { get; }
    MapArea TargetMap { get; }
    EntryPoint TargetEntryPoint { get; }
    Transform TransitionPoint { get; }
    Collider2D TransitionArea { get; }
    bool CanKuneKuneUse { get; }
}

/// <summary>
/// 맵 이동을 유발하는 모든 오브젝트에 상속되는 추상 클래스
/// </summary>
public abstract class MapTransition : MonoBehaviour, IKuneKuneMapRoute
{
    [Header("Target Map Settings")]
    [SerializeField] protected MapArea targetMap;
    [SerializeField] protected EntryPoint targetEntryPoint;

    public MapArea TargetMap => targetMap;
    public EntryPoint TargetEntryPoint => targetEntryPoint;
    
    // 💡 MapArea 객체의 자식으로 존재해야 정상적으로 SourceMap을 찾아냅니다.
    public MapArea SourceMap => GetComponentInParent<MapArea>();
    public Transform TransitionPoint => transform;
    public Collider2D TransitionArea => GetComponent<Collider2D>();
    public virtual bool CanKuneKuneUse => true;

    /// <summary>
    /// 맵 이동 메서드. 실제 이동 로직은 MapManager가 담당.
    /// </summary>
    protected void ExecuteTransition()
    {
        if (targetMap == null || targetEntryPoint == null)
        {
            Debug.LogWarning($"{name}: TargetMap 또는 TargetEntryPoint 설정이 필요합니다.");
            return;
        }

        // 💡 핵심 구조 변경: MapManager에는 객체를 통째로 넘기지 않고,
        // Enum 타입(mapType)과 문자열(entryId) 정보만 안전하게 분리해서 전달합니다.
        MapManager.Instance?.ChangeMap(targetMap.mapType, targetEntryPoint.EntryID);
    }
}