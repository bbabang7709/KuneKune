using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CompositeCollider2D))]
public class SunlightAreaRoot : MonoBehaviour
{
    [Header("�ν����Ϳ��� ���� ����")]
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
                // �ʱ�ȭ
                co.enabled = true;
                co.compositeOperation = Collider2D.CompositeOperation.Merge;
            }
        }

        _collider.isTrigger = true;
        _collider.geometryType = CompositeCollider2D.GeometryType.Polygons;
        LayerMask playerMask = LayerMask.GetMask("Player");
        _collider.excludeLayers = ~playerMask;  // �÷��̾ �ν�
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (MapManager.Instance == null || MapManager.Instance.CurrentMap == null)
            return;

        // �÷��̾ �ִ� ������ Ȯ��
        // if (MapManager.Instance.CurrentMap.mapType != _mapID)
        //     return;

        Debug.Log("�÷��̾�� ���ռ� ��~ ��ȣ~~");
        _player.IsExposedToSunlight = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (MapManager.Instance == null || MapManager.Instance.CurrentMap == null)
            return;

        // �÷��̾ �ִ� ������ Ȯ��
        // if (MapManager.Instance.CurrentMap.MapID != _mapID)
        //     return;

        Debug.Log("��Ÿ�� D�� �����ϳ׿�! �� ���ռ��� �ϴ� ���ƿ�?");
        _player.IsExposedToSunlight = false;
    }
}