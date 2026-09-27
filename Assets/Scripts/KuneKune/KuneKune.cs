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

    private ECharacterState _preCutsceneState;  // 컷신 재생 중 이동을 멈추기 위해, 이전의 행동 상태를 저장해둘 변수

    private bool _isCatchingPlayer = false; // 공포 연출 중복 실행 방지용 플래그

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
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.GetComponent<PlayerController>();
            _playerCollider = _player.GetComponent<Collider2D>();
        }

        _rigidbody.gravityScale = 0f;
        _rigidbody.freezeRotation = true;   // z축 고정

        int allowedLayerMask = LayerMask.GetMask("Player");
        _collider.excludeLayers = ~allowedLayerMask;

        Despawn();
    }

    public static KuneKune Instantiate()
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/Character/KuneKune");
        KuneKune kuneKune = Instantiate(prefab).GetComponent<KuneKune>();
        return kuneKune;
    }

    public void Spawn()
    {
        if (State != ECharacterState.Unspawned)
            return;

        #region 플레이어랑 가까운 사물 찾기
        if (MapManager.Instance.KunekuneSpawnPoints.Count == 0)
        {
            Debug.Log("현재 맵에서 쿠네쿠네가 소환될 수 있는 사물이 없네요.");
            
            // TEST용 폴백 로직 유지
            Transform tr = GameObject.Find("KuneKune_ables")?.transform;
            if (tr != null)
            {
                Debug.Log("테스트를 위해 아무 KuneKune_ables에서 소환할게요");
                Transform[] spawnPoints = tr.GetComponentsInChildren<Transform>();
                foreach (Transform sp in spawnPoints)
                {
                    if (sp == tr) continue;
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
            float rightValue = Vector3.Dot(direction, _player.transform.right); 

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestRightValue = rightValue;
                closest = child.gameObject;
            }
            else if (Mathf.Abs(distance - closestDistance) <= 0.01f && rightValue > closestRightValue)
            {
                closestRightValue = rightValue;
                closest = child.gameObject;
            }
        }

        if (closest == null)
        {
            Debug.LogError("쿠네쿠네가 플레이어와 가까운 사물을 못 찾았습니다.");
            return;
        }
        #endregion

        _spawnAnimationName = Regex.Replace(closest.name, @"_0(?: \(\d+\))?$", "");
        SpawnObject = closest;
        closest.SetActive(false);   
        Spawn(closest.transform.position);
    }

    public void Spawn(Vector3 position, bool runImmediately = false)
    {
        gameObject.SetActive(true);
        SoundManager.Instance?.PlayBGM("BGM_KuneKune", 0.01f);

        _currentMap = MapManager.Instance?.CurrentMap;
        _lastPlayerMap = _currentMap;
        _mapRoutes.Clear();

        State = runImmediately ? ECharacterState.Run : ECharacterState.Spawning;
        
        SetPositionAndDirection(position);

        // 등장 경로와 관계없이 더위 게이지와 연결된 연출을 최대로 올립니다.
        _player.GetComponent<PlayerMovement>().SetHeatToMax();

        if (_fullScreenGlitchMaterial != null)
        {
            _fullScreenGlitchMaterial.SetFloat(IntensityId, Mathf.Clamp01(intensity)); 
        }
    }

    // 발을 바닥 높이에 맞춰 배치하고 플레이어 쪽을 바라봅니다.
    private void SetPositionAndDirection(Vector3 floorPosition)
    {
        transform.position = GetFloorAlignedPosition(floorPosition);

        float xDifference = _player.transform.position.x - floorPosition.x;
        _direction = xDifference > 0f ? 1f : -1f;
        _spriteRenderer.flipX = _direction < 0f;
    }

    private Vector3 GetFloorAlignedPosition(Vector3 floorPosition)
    {
        float bottomOffset = transform.position.y - _collider.bounds.min.y;
        floorPosition.y += bottomOffset;
        return floorPosition;
    }

    private Vector3 GetFloorPositionFromDoorSpawnPoint(Vector3 spawnPosition)
    {
        float playerBottomOffset = _player.transform.position.y - _playerCollider.bounds.min.y;
        spawnPosition.y -= playerBottomOffset;
        return spawnPosition;
    }

    public void Despawn()
    {
        _mapRoutes.Clear();
        _currentMap = null;
        _lastPlayerMap = null;
        gameObject.SetActive(false);
        
        if (_fullScreenGlitchMaterial != null)
        {
            _fullScreenGlitchMaterial.SetFloat(IntensityId, 0f);    
        }
        State = ECharacterState.Unspawned;

        var currentArea = MapManager.Instance?.CurrentMap;
        if (currentArea != null)
        {
            SoundManager.Instance?.PlayBGM(currentArea.BgmID);
            if (currentArea.IsInside) SoundManager.Instance?.FadeBGMVolume(0.35f);
            else SoundManager.Instance?.FadeBGMVolume(1f);
        }
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

    private void ChasePlayerAcrossMaps()
    {
        if (DialogSystem.Instance != null && DialogSystem.Instance.IsDialogPlaying) return;
        
        MapArea playerMap = MapManager.Instance.CurrentMap;

        if (playerMap == null)
            return;

        if (_currentMap == null)
            _currentMap = playerMap;

        if (_currentMap == playerMap)
        {
            _mapRoutes.Clear();
            _lastPlayerMap = playerMap;
            MoveHorizontallyTowards(_player.transform.position.x);
            return;
        }

        if (_lastPlayerMap != playerMap)
        {
            _lastPlayerMap = playerMap;
            _mapRoutes = MapManager.Instance.FindRoute(_currentMap, transform.position, playerMap, _player.transform.position);
        }

        if (_mapRoutes.Count > 0)
            ChaseMapTransition();
    }

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

    private void MoveThroughMapTransition()
    {
        IKuneKuneMapRoute currentRoute = _mapRoutes.Peek();
        MapArea targetMap = currentRoute.TargetMap;
        EntryPoint targetEntryPoint = currentRoute.TargetEntryPoint;

        if (targetMap == null || targetEntryPoint == null)
        {
            Debug.LogWarning("쿠네쿠네가 이동할 문 건너편 위치가 설정되지 않았습니다.", this);
            _mapRoutes.Clear();
            return;
        }

        Vector3 floorPosition = GetFloorPositionFromDoorSpawnPoint(targetEntryPoint.transform.position);

        _currentMap = targetMap;
        SetPositionAndDirection(floorPosition);
        _mapRoutes.Dequeue();
    }

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

        if (collision.gameObject.CompareTag("Player") && !_isCatchingPlayer)
        {
            _isCatchingPlayer = true;
            if (HouseChase.Instance != null && HouseChase.Instance.IsChaseActive)
            {
                HouseChase.Instance.EndChase();
                _isCatchingPlayer = false;
                return;
            }

            StartCoroutine(CatchPlayerSequence());
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (State != ECharacterState.Run)
            return;

        if (collision.gameObject.CompareTag("Player") && !_isCatchingPlayer)
        {
            _isCatchingPlayer = true;
            if (HouseChase.Instance != null && HouseChase.Instance.IsChaseActive)
            {
                HouseChase.Instance.EndChase();
                return;
            }

            StartCoroutine(CatchPlayerSequence());
        }
    }

    private void PauseKunekune()
    {
        if (State == ECharacterState.Unspawned) return;

        _preCutsceneState = State;
        State = ECharacterState.Idle;
    }

    private void ResumeKunekune()
    {
        if (State == ECharacterState.Unspawned) return;

        State = _preCutsceneState;
    }

    private IEnumerator CatchPlayerSequence()
    {
        Debug.Log("플레이어가 쿠네쿠네에게 잡혔다! 데스씬 연출 시작");

        State = ECharacterState.Idle;

        ControlLockManager.Instance?.AddInputLock(this);
        ControlLockManager.Instance?.AddMovementLock(this);

        SoundManager.Instance?.SetBGMVolume(0f);
        SoundManager.Instance?.PlaySFX("KuneKune_Jumpscare");   // 4초 지속

        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.ShowJumpscareRoutine(0.05f)); // 0.1초 경과
        }

        yield return new WaitForSecondsRealtime(1.5f);    // 1.1초 경과

        if (UIManager.Instance != null)
        {
            yield return UIManager.Instance.FadeOut(0.2f);  // 1.3초 경과
        }

        yield return new WaitForSecondsRealtime(1.5f);

        SaveManager.Instance?.Load(true);
    }

#if UNITY_EDITOR
    void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        if (CutsceneManager.Instance != null)
        {
            CutsceneManager.Instance.OnCutsceneStart += PauseKunekune;
            CutsceneManager.Instance.OnCutsceneEnd += ResumeKunekune;
        }
    }

    void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

        if (CutsceneManager.Instance != null)
        {
            CutsceneManager.Instance.OnCutsceneStart -= PauseKunekune;
            CutsceneManager.Instance.OnCutsceneEnd -= ResumeKunekune;
        }
    }

    void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            if (_fullScreenGlitchMaterial != null)
            {
                _fullScreenGlitchMaterial.SetFloat(IntensityId, 0f);    
            }
        }
    }
#endif
}
