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

        // MapManager 객체를 통한 맵 변경
        MapManager.Instance?.ChangeMap(targetMap, targetEntryPoint);
    }
}
