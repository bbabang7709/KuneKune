using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public enum ECharacterState
{
    Unspawned,  // 아직 소환되지 않은 상태
    Spawning,   // 쿠네쿠네가 사물에서 나타나는 상태
    Idle,
    Run,
}

public class KuneKune : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    private Rigidbody2D _rigidbody;
    private Collider2D _collider;
    private PlayerController _player;
    private Collider2D _playerCollider;

    private ECharacterState _state;
    public ECharacterState State
    {
        get => _state;
        set
        {
            if (_state != value)
            {
                _state = value;
                UpdateAnimation();
            }
        }
    }

    private float _direction;
    private float _speed = 5f;

    private Queue<IKuneKuneMapRoute> _mapRoutes = new();
    private MapArea _currentMap;
    private MapArea _lastPlayerMap;

    private string _spawnAnimationName = "";

    [Header("FullScreenGlitch")]
    [SerializeField] Material _fullScreenGlitchMaterial;
    [SerializeField, Range(0f, 1f)] float intensity = 0.3f; // 0(정상 화면) ~ 1(글리치 강도 최대)
    readonly int IntensityId = Shader.PropertyToID("_Intensity");

    public GameObject SpawnObject { get; private set; }

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        _playerCollider = _player.GetComponent<Collider2D>();

        _rigidbody.gravityScale = 0f;
        _rigidbody.freezeRotation = true;   // z축 고정

        int allowedLayerMask = LayerMask.GetMask("Player");
        _collider.excludeLayers = ~allowedLayerMask;

        Despawn();
    }

    /// <summary>
    /// 정적 함수입니다. 쿠네쿠네 프리팹을 인스턴스화하고, KuneKune 컴포넌트를 반환합니다.
    /// 쿠네쿠네를 현재 씬에서 처음 인스턴스화 할 때 사용하세요.
    /// </summary>
    /// <returns></returns>
    public static KuneKune Instantiate()
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/Character/KuneKune");
        KuneKune kuneKune = Instantiate(prefab).GetComponent<KuneKune>();
        return kuneKune;
    }

    /// <summary>
    /// 플레이어와 가까운 사물에 쿠네쿠네를 소환합니다.
    /// </summary>
    public void Spawn()
    {
        // 중복 방지
        if (State != ECharacterState.Unspawned)
            return;

        #region 플레이어랑 가까운 사물 찾기
        if (MapManager.Instance.KunekuneSpawnPoints.Count == 0)
        {
            Debug.Log("현재 맵에서 쿠네쿠네가 소환될 수 있는 사물이 없네요.");
            //return;

            // TODO: TEST용 삭제 필요
            {
                Debug.Log("테스트를 위해 아무 KuneKune_ables에서 소환할게요");
                Transform tr = GameObject.Find("KuneKune_ables").transform;
                Transform[] spawnPoints = tr.GetComponentsInChildren<Transform>();
                foreach (Transform sp in spawnPoints)
                {
                    if (sp == tr)   // 부모 제외
                        continue;

                    SpawnObject = sp.gameObject;
                    MapManager.Instance.KunekuneSpawnPoints.Add(sp);
                }
            }
        }
        
        GameObject closest = null;
        float closestDistance = Mathf.Infinity;
        float closestRightValue = Mathf.NegativeInfinity;

        foreach (Transform child in MapManager.Instance.KunekuneSpawnPoints)
        {
            Vector3 direction = child.position - _player.transform.position;
            float distance = direction.magnitude;
            float rightValue = Vector3.Dot(direction, _player.transform.right); // 플레이어 기준 오른쪽으로 얼마나 있는지

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestRightValue = rightValue;
                closest = child.gameObject;
            }
            // 거리가 거의 같으면 플레이어 기준 오른쪽을 선택
            else if (Mathf.Abs(distance - closestDistance) <= 0.01f && rightValue > closestRightValue)
            {
                closestRightValue = rightValue;
                closest = child.gameObject;
            }
        }

        // 플레이어와 가까운 사물이 없다면 쿠네쿠네를 활성화하지 않음
        if (closest == null)
        {
            Debug.LogError("쿠네쿠네가 플레이어와 가까운 사물을 못 찾았습니다.");
            return;
        }
        #endregion

        _spawnAnimationName = Regex.Replace(closest.name, @"_0(?: \(\d+\))?$", "");
        SpawnObject = closest;
        closest.SetActive(false);   // 쿠네쿠네가 소환되면 사물은 비활성화. TODO: 맵 나갔다 오면 다시 사물 활성화
        Spawn(closest.transform.position);
    }

    /// <summary>
    /// 지정한 바닥 위치에 쿠네쿠네를 소환합니다. 이미 소환된 상태라면 위치만 이동합니다.
    /// </summary>
    /// <param name="position">쿠네쿠네를 소환할 바닥 위치입니다.</param>
    public void Spawn(Vector3 position)
    {
        gameObject.SetActive(true);

        bool isFirstSpawn = State == ECharacterState.Unspawned;

        // 쿠네쿠네가 사물에서 나타나는 상태가 아니라면 바로 달리기 시작
        State = isFirstSpawn ? ECharacterState.Spawning : ECharacterState.Run;
        if (isFirstSpawn)
        {
            _currentMap = MapManager.Instance?.CurrentMap;
            _lastPlayerMap = _currentMap;
            _mapRoutes.Clear();
        }
        
        transform.position = GetFloorAlignedPosition(position);

        float xDifference = _player.transform.position.x - position.x;
        _direction = xDifference > 0f ? 1f : -1f;
        _spriteRenderer.flipX = _direction < 0f;

        _fullScreenGlitchMaterial.SetFloat(IntensityId, Mathf.Clamp01(intensity));  // 0(정상 화면) ~ 1(글리치 강도 최대)
    }

    /// <summary>
    /// 쿠네쿠네 콜라이더의 맨 아래가 지정한 바닥 높이에 닿도록 위치를 올립니다.
    /// </summary>
    private Vector3 GetFloorAlignedPosition(Vector3 floorPosition)
    {
        // 쿠네쿠네 피벗에서 콜라이더 맨 아래까지의 거리를 계산합니다.
        float bottomOffset = transform.position.y - _collider.bounds.min.y;
        floorPosition.y += bottomOffset;
        return floorPosition;
    }

    /// <summary>
    /// 문에 저장된 플레이어 도착 위치를 실제 바닥 위치로 바꿉니다.
    /// </summary>
    private Vector3 GetFloorPositionFromDoorSpawnPoint(Vector3 spawnPosition)
    {
        // 문의 Spawn Point는 플레이어 피벗을 놓을 위치이므로,
        // 플레이어 피벗에서 콜라이더 맨 아래까지의 거리만큼 내려 바닥을 찾습니다.
        float playerBottomOffset = _player.transform.position.y - _playerCollider.bounds.min.y;
        spawnPosition.y -= playerBottomOffset;
        return spawnPosition;
    }

    /// <summary>
    /// 소환한 쿠네쿠네를 비활성화하고, 글리치 효과를 끕니다.
    /// </summary>
    public void Despawn()
    {
        _mapRoutes.Clear();
        _currentMap = null;
        _lastPlayerMap = null;
        gameObject.SetActive(false);
        _fullScreenGlitchMaterial.SetFloat(IntensityId, 0f);    // 0(정상 화면)
        State = ECharacterState.Unspawned;
    }

    private void UpdateAnimation()
    {
        switch (_state)
        {
            case ECharacterState.Spawning:
                _animator.Play(_spawnAnimationName);
                break;
            case ECharacterState.Run:
                _animator.Play("Run");
                break;
        }
    }

    private void FixedUpdate()
    {
        switch (_state)
        {
            case ECharacterState.Spawning:
                // 스폰 애니메이션이 끝나면 Run 상태로 전환
                if (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
                    State = ECharacterState.Run;
                break;
            case ECharacterState.Idle:
                break;
            case ECharacterState.Run:
                ChasePlayerAcrossMaps();
                break;
        }
    }

    /// <summary>
    /// 플레이어와 같은 맵이면 바로 쫓아가고, 다른 맵이면 가장 짧은 문 경로를 찾습니다.
    /// </summary>
    private void ChasePlayerAcrossMaps()
    {
        MapArea playerMap = MapManager.Instance.CurrentMap;

        if (playerMap == null)
            return;

        if (_currentMap == null)
            _currentMap = playerMap;

        // 플레이어와 쿠네쿠네의 맵이 같다
        if (_currentMap == playerMap)
        {
            _mapRoutes.Clear();
            _lastPlayerMap = playerMap;
            MoveHorizontallyTowards(_player.transform.position.x);
            return;
        }

        // 플레이어가 다른 맵으로 또 이동했다면 새 목적지까지의 최단 경로를 다시 찾습니다.
        if (_lastPlayerMap != playerMap)
        {
            _lastPlayerMap = playerMap;
            _mapRoutes = MapManager.Instance.FindRoute(_currentMap, transform.position, playerMap, _player.transform.position);
        }

        if (_mapRoutes.Count > 0)
            ChaseMapTransition();
    }

    /// <summary>
    /// 최단 경로에서 선택된 문이나 출구까지 직접 달려갑니다.
    /// </summary>
    private void ChaseMapTransition()
    {
        IKuneKuneMapRoute currentRoute = _mapRoutes.Peek();
        if (currentRoute.TransitionPoint == null)
        {
            Debug.LogWarning("쿠네쿠네가 이동할 문 위치가 사라졌습니다.", this);
            _mapRoutes.Clear();
            return;
        }

        Collider2D transitionArea = currentRoute.TransitionArea;
        bool hasTransitionArea = transitionArea != null;
        bool reachedTransition = hasTransitionArea && _collider.bounds.Intersects(transitionArea.bounds);

        float targetX = currentRoute.TransitionPoint.position.x;
        bool reachedPositionWithoutArea = hasTransitionArea == false && Mathf.Abs(targetX - transform.position.x) <= 0.01f;

        if (reachedTransition || reachedPositionWithoutArea)
        {
            MoveThroughMapTransition();
            return;
        }

        MoveHorizontallyTowards(targetX);
    }

    /// <summary>
    /// 문이나 출구 영역에 도착한 뒤 연결된 맵으로 이동합니다.
    /// </summary>
    private void MoveThroughMapTransition()
    {
        IKuneKuneMapRoute currentRoute = _mapRoutes.Peek();
        MapArea targetMap = currentRoute.TargetMap;
        EntryPoint targetEntryPoint = currentRoute.TargetEntryPoint;

        if (targetMap == null || targetEntryPoint == null || targetEntryPoint.SpawnPoint == null)
        {
            Debug.LogWarning("쿠네쿠네가 이동할 문 건너편 위치가 설정되지 않았습니다.", this);
            _mapRoutes.Clear();
            return;
        }

        Vector3 floorPosition = GetFloorPositionFromDoorSpawnPoint(targetEntryPoint.SpawnPoint.position);

        _currentMap = targetMap;
        Spawn(floorPosition);
        _mapRoutes.Dequeue();
    }

    /// <summary>
    /// 현재 높이는 유지하고 지정한 가로 위치를 향해 움직입니다.
    /// </summary>
    private void MoveHorizontallyTowards(float targetX)
    {
        float xDifference = targetX - transform.position.x;
        if (Mathf.Abs(xDifference) <= 0.01f)
            return;

        _direction = Mathf.Sign(xDifference);
        _spriteRenderer.flipX = _direction < 0f;

        Vector3 targetPosition = new Vector3(targetX, transform.position.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, _speed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (State != ECharacterState.Run)
            return;

        // 플레이어를 잡았는 지 확인
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("플레이어가 쿠네쿠네에게 잡혔다! 쿠네쿠네 멈춤");
            State = ECharacterState.Idle;
        }
    }

#if UNITY_EDITOR
    void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    // 에디터에서 플레이 모드가 끝나면 글리치 강도 초기화
    void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            _fullScreenGlitchMaterial.SetFloat(IntensityId, 0f);    // 0(정상 화면)
        }
    }
#endif
}
