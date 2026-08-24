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

    private Vector3 _direction;
    private float _speed = 5f;

    private string _spawnAnimationName = "";

    [Header("FullScreenGlitch")]
    [SerializeField] Material _fullScreenGlitchMaterial;
    [SerializeField, Range(0f, 1f)] float intensity = 0.3f; // 0(정상 화면) ~ 1(글리치 강도 최대)
    readonly int IntensityId = Shader.PropertyToID("_Intensity");

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

        _rigidbody.gravityScale = 0f;
        _rigidbody.freezeRotation = true;   // z축 고정

        // 플레이어와 충돌 방지
        int playerLayerMask = LayerMask.GetMask("Player");
        _collider.excludeLayers = playerLayerMask;

        Despawn();
        //Spawn(Vector3.zero);    // TODO: test용이다. 제거 필요.
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
                    MapManager.Instance.KunekuneSpawnPoints.Add(sp);
                }
            }
        }

        #region 플레이어랑 가까운 사물 찾기
        GameObject closest = null;
        float closestDistance = Mathf.Infinity;
        float closestRightValue = Mathf.NegativeInfinity;

        foreach (Transform child in MapManager.Instance.KunekuneSpawnPoints)
        {
            if (child.name == "KuneKune_ables") continue;   // 부모 오브젝트는 제외

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

        // 쿠네쿠네 위치를 사물이 놓인 바닥 높이에 맞도록 올린다
        //SpriteRenderer closestRenderer = closest.GetComponent<SpriteRenderer>();
        //float floorY = closestRenderer != null ? closestRenderer.bounds.min.y : closest.transform.position.y;
        //float offset = transform.position.y - _collider.bounds.min.y;
        //transform.position = new Vector3(closest.transform.position.x, floorY + offset, closest.transform.position.z);
        #endregion

        _spawnAnimationName = Regex.Replace(closest.name, @"_0(?: \(\d+\))?$", "");
        closest.SetActive(false);   // 쿠네쿠네가 소환되면 사물은 비활성화. TODO: 맵 나갔다 오면 다시 사물 활성화
        Spawn(closest.transform.position);
    }

    /// <summary>
    /// 원하는 위치에 쿠네쿠네를 소환합니다. 이미 소환된 상태라면 위치만 이동합니다.
    /// </summary>
    /// <param name="position"></param>
    public void Spawn(Vector3 position)
    {
        gameObject.SetActive(true);

        // 쿠네쿠네가 사물에서 나타나는 상태가 아니라면 바로 달리기 시작
        State = State == ECharacterState.Unspawned ? ECharacterState.Spawning : ECharacterState.Run;

        transform.position = position;
        _direction = (_player.transform.position - position).normalized;
        _spriteRenderer.flipX = _direction.x < 0;
        
        _fullScreenGlitchMaterial.SetFloat(IntensityId, Mathf.Clamp01(intensity));  // 0(정상 화면) ~ 1(글리치 강도 최대)
        
    }

    /// <summary>
    /// 소환한 쿠네쿠네를 비활성화하고, 글리치 효과를 끕니다.
    /// </summary>
    public void Despawn()
    {
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
                ChasePlayer();
                break;
        }
    }

    /// <summary>
    /// 쿠네쿠네가 플레이어를 쫓아갑니다.
    /// </summary>
    private void ChasePlayer()
    {
        // 플레이어를 잡았는 지 확인
        float distance = Mathf.Abs(_player.transform.position.x - transform.position.x);
        if (distance < 1.1f)
        {
            Debug.Log("플레이어가 쿠네쿠네에게 잡혔다! 쿠네쿠네 멈춤");
            State = ECharacterState.Idle;
            return;
        }

        _direction = (_player.transform.position - transform.position).normalized;
        _direction = (_direction.x > 0) ? Vector3.right : Vector3.left;
        _spriteRenderer.flipX = _direction.x < 0;
        transform.position += _direction * _speed * Time.fixedDeltaTime;
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
