using UnityEngine;

/// <summary>
/// 맵을 구분짓는 큰 틀(주로 건물 단위)
/// </summary>
public enum EMapType
{
    Outside,
    House,
    School
}

/// <summary>
/// Map의 정보를 담는 클래스 (EMapType + MapID)
/// </summary>
public class MapArea : MonoBehaviour
{
    [Header("Map Info")]
    [SerializeField] private string _mapID;
    [SerializeField] private EMapType _mapType;
    [SerializeField] private Transform _kuneKune_ables;

    /// <summary> 맵 식별 문자열 (ex. school_hallway, school_classroom, ...) </summary>
    public string MapID => _mapID;
    /// <summary> 맵 종류 Enum (Outside, School, House) </summary>
    public EMapType MapType => _mapType;
    /// <summary> 쿠네쿠네 스폰 지점 </summary>
    public Transform KuneKune_ables => _kuneKune_ables;

    // // 맵 콜라이더에 들어가면 현재 위치한 맵 정보 갱신
    // private void OnTriggerEnter2D(Collider2D collision)
    // {
    //     if (!collision.CompareTag("Player")) return;

    //     MapManager.Instance.SetCurrentMap(this);
    // }
}
