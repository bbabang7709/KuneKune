using UnityEngine;

/// <summary>
/// 맵 이동을 유발하는 모든 오브젝트에 상속되는 추상 클래스
/// </summary>
public abstract class MapTransition : MonoBehaviour
{
    [Header("Target Map Settings")]
    [SerializeField] protected MapArea targetMap;
    [SerializeField] protected EntryPoint targetEntryPoint;

    public MapArea TargetMap => targetMap;
    public EntryPoint TargetEntryPoint => targetEntryPoint;

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
