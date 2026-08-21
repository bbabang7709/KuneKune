using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CompositeCollider2D))]
public class SunlightAreaRoot : MonoBehaviour
{
    [Header("인스펙터에서 직접 설정")]
    [SerializeField] private string _mapID;

    private CompositeCollider2D _collider;
    private PlayerMovement _player;

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        _collider = GetComponent<CompositeCollider2D>();
        _player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();

        foreach (Transform child in transform)
        {
            Collider2D co = child.GetComponent<Collider2D>();
            if (co != null)
            {
                // 초기화
                co.enabled = true;
                co.compositeOperation = Collider2D.CompositeOperation.Merge;
            }
        }

        _collider.isTrigger = true;
        _collider.geometryType = CompositeCollider2D.GeometryType.Polygons;
        LayerMask playerMask = LayerMask.GetMask("Player");
        _collider.excludeLayers = ~playerMask;  // 플레이어만 인식
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (MapManager.Instance == null || MapManager.Instance.CurrentMap == null)
            return;

        // 플레이어가 있는 맵인지 확인
        if (MapManager.Instance.CurrentMap.MapID != _mapID)
            return;

        Debug.Log("플레이어는 광합성 중~ 야호~~");
        _player.IsExposedToSunlight = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (MapManager.Instance == null || MapManager.Instance.CurrentMap == null)
            return;

        // 플레이어가 있는 맵인지 확인
        if (MapManager.Instance.CurrentMap.MapID != _mapID)
            return;

        Debug.Log("비타민 D가 부족하네요! 왜 광합성을 하다 말아요?");
        _player.IsExposedToSunlight = false;
    }
}