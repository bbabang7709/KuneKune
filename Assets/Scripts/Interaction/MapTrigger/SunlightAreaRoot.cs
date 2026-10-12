using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CompositeCollider2D))]
public class SunlightAreaRoot : MonoBehaviour
{
    private CompositeCollider2D _collider;
    private PlayerMovement _player;

    private bool _isPlayerInside;

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        _isPlayerInside = false;
        _collider = GetComponent<CompositeCollider2D>();
        _player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();

        foreach (Transform child in transform)
        {
            Collider2D co = child.GetComponent<Collider2D>();
            if (co != null)
            {
                co.enabled = true;
                co.compositeOperation = Collider2D.CompositeOperation.Merge;
            }
        }

        _collider.isTrigger = true;
        _collider.geometryType = CompositeCollider2D.GeometryType.Polygons;
        LayerMask playerMask = LayerMask.GetMask("Player");
        _collider.excludeLayers = ~playerMask;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (MapManager.Instance == null || MapManager.Instance.CurrentMap == null)
            return;

        // if (MapManager.Instance.CurrentMap.mapType != _mapID)
        //     return;

        _isPlayerInside = true;
        _player.IsExposedToSunlight = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (MapManager.Instance == null || MapManager.Instance.CurrentMap == null)
            return;

        // if (MapManager.Instance.CurrentMap.MapID != _mapID)
        //     return;
        
        _isPlayerInside = false;
        _player.IsExposedToSunlight = false;
    }

    public void Deactivate()
    {
        if (_isPlayerInside)
        {
            _isPlayerInside = false;
            _player.IsExposedToSunlight = false;
        }

        gameObject.SetActive(false);
    }
}
