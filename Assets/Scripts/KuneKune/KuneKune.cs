using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum ECharacterState
{
    Unspawned,  // 아직 소환되지 않은 상태
    Spawning,   // 쿠네쿠네가 사물에서 나타나는 상태
    Idle,
    Run,
    WaitingAfterSpawn, // 일어난 뒤 추격을 시작하기 전 대기
}

public class KuneKune : MonoBehaviour
{
    public const float DefaultMoveSpeed = 5f;
    private const float PostSpawnWaitSeconds = 0.2f;

    public SpriteRenderer SpriteRenderer { get; private set; }
    private Animator _animator;
    private Rigidbody2D _rigidbody;
    private Collider2D _collider;
    private Bounds _spawnColliderBounds;
    private PlayerController _player;
    private Collider2D _playerCollider;

    [Header("Spawn")]
    [Tooltip("스폰 시 플레이어와 쿠네쿠네의 콜라이더 사이에 확보할 최소 간격입니다.")]
    [SerializeField, Min(0.01f)] private float _spawnClearance = 0.1f;

    private ECharacterState _state;
    // 대사나 컷씬으로 잠시 Idle이 되어도 추격 중이라는 사실은 유지합니다.
    public bool IsChaseActive { get; private set; }
    public ECharacterState State
    {
        get => _state;
        set
        {
            if (_state != value)
            {
                _state = value;
                if (value == ECharacterState.Run)
                    IsChaseActive = true;
                else if (value == ECharacterState.Unspawned)
                    IsChaseActive = false;
                UpdateAnimation();
            }
        }
    }

    private float _direction;
    [SerializeField] private float _speed = DefaultMoveSpeed;

    private Queue<IKuneKuneMapRoute> _mapRoutes = new();
    private MapArea _currentMap;
    private MapArea _lastPlayerMap;
    private float? _mapTransitionWaitRemaining;
    private float _postSpawnWaitRemaining;

    public string SpawnAnimationName { get; private set; } = "";

    [Header("FullScreenGlitch")]
    [SerializeField] Material _fullScreenGlitchMaterial;
    [SerializeField, Range(0f, 1f)] float intensity = 0.3f; // 0(정상 화면) ~ 1(글리치 강도 최대)
    readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int GlitchTimeId = Shader.PropertyToID("_GlitchTime");

    public GameObject SpawnObject { get; private set; }

    private ECharacterState _preCutsceneState;  // 컷신 재생 중 이동을 멈추기 위해, 이전의 행동 상태를 저장해둘 변수

    private bool _isInDeathSequence;
    private bool _isCatchingPlayer = false; // 공포 연출 중복 실행 방지용 플래그

    private bool IsGameplayPaused => Time.timeScale <= 0f ||
        (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPaused) ||
        (MapManager.Instance != null && MapManager.Instance.IsChangingMap);

    [SerializeField] private KuneKuneFirstEncounterCutsceneController _firstEncounterCutscene;

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        SpriteRenderer = GetComponent<SpriteRenderer>();
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

        // 비활성 콜라이더의 bounds는 비어 있으므로 숨기기 전에 몸체 기준 영역을 보관합니다.
        _spawnColliderBounds = _collider.bounds;
        _spawnColliderBounds.center -= transform.position;
        Despawn();

        if (_firstEncounterCutscene == null)
            _firstEncounterCutscene = GetComponentInChildren<KuneKuneFirstEncounterCutsceneController>(true);
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
        if (MapManager.Instance == null || _player == null || _playerCollider == null)
            return;

        #region 플레이어와 떨어진 사물 중 가장 가까운 사물 찾기
        if (MapManager.Instance.KunekuneSpawnPoints.Count == 0)
        {
            Debug.Log("현재 맵에서 쿠네쿠네가 소환될 수 있는 사물이 없네요.");
            return;
        }

        // 같은 프레임에 바뀐 위치와 나이별 콜라이더 크기도 선택에 반영합니다.
        Physics2D.SyncTransforms();
        Bounds playerBounds = _playerCollider.bounds;
        GameObject closest = null;
        float closestDistance = Mathf.Infinity;
        float closestRightValue = Mathf.NegativeInfinity;

        foreach (Transform child in MapManager.Instance.KunekuneSpawnPoints)
        {
            if (child == null || !child.gameObject.activeInHierarchy ||
                !IsSpawnPositionSafe(child.position, playerBounds))
                continue;

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
            // 안전한 후보가 생기면 다음 스폰 요청에서 다시 선택합니다.
            return;
        }
        #endregion

        SpawnAnimationName = Regex.Replace(closest.name, @"_0(?: \(\d+\))?$", "");

        SpawnObject = closest;
        closest.SetActive(false);
        Spawn(closest.transform.position);
    }

    public void Spawn(Vector3 position, bool runImmediately = false)
    {
        // 즉시 등장하는 이벤트는 첫 조우 연출을 건너뛰고 호출한 컷씬의 흐름을 유지합니다.
        bool playFirstEncounter = !runImmediately && MapManager.Instance.CurrentMapType != EMapType.Chase_Ricefield
            && _firstEncounterCutscene != null && !_firstEncounterCutscene.HasPlayed;

        // // 활성화와 로딩 대기 전에 숨겨 첫 조우 시작 시 이전 스프라이트가 보이지 않게 합니다.
        SpriteRenderer.enabled = !playFirstEncounter;
        if (playFirstEncounter && SpawnObject != null)
            SpawnObject.SetActive(true);

        gameObject.SetActive(true);
        bool isTrueEndingChase = MapManager.Instance.CurrentMapType == EMapType.Chase_Ricefield &&
            SaveManager.Instance != null && SaveManager.Instance.IsTrueEndingUnlocked;
        SoundManager.Instance?.PlayBGM(isTrueEndingChase ? "DontLeave" : "BGM_KuneKune", 0.01f);

        _currentMap = MapManager.Instance?.CurrentMap;
        _lastPlayerMap = _currentMap;
        _mapRoutes.Clear();
        _mapTransitionWaitRemaining = null;
        _postSpawnWaitRemaining = 0f;

        SetPositionAndDirection(position);

        // 첫 등장 때는 쿠네쿠네 컷씬
        if (playFirstEncounter)
            _firstEncounterCutscene.Play();
        else
            State = runImmediately ? ECharacterState.Run : ECharacterState.Spawning;
        
        // 등장 경로와 관계없이 더위 게이지와 연결된 연출을 최대로 올립니다.
        PlayerMovement p = _player.GetComponent<PlayerMovement>();
        if (p.CurrentHeat >= p.MaxHeat)
            p.SetCurrentHeat(p.MaxHeat);

        if (_fullScreenGlitchMaterial != null)
            _fullScreenGlitchMaterial.SetFloat(IntensityId, Mathf.Clamp01(intensity)); 
    }

    // 발을 바닥 높이에 맞춰 배치하고 플레이어 쪽을 바라봅니다.
    private void SetPositionAndDirection(Vector3 floorPosition)
    {
        transform.position = GetFloorAlignedPosition(floorPosition);

        float xDifference = _player.transform.position.x - floorPosition.x;
        _direction = xDifference > 0f ? 1f : -1f;
        SpriteRenderer.flipX = _direction < 0f;
    }

    private Vector3 GetFloorAlignedPosition(Vector3 floorPosition)
    {
        floorPosition.y -= _spawnColliderBounds.min.y;
        return floorPosition;
    }

    private bool IsSpawnPositionSafe(Vector3 floorPosition, Bounds playerBounds)
    {
        Bounds spawnBounds = _spawnColliderBounds;
        spawnBounds.center += GetFloorAlignedPosition(floorPosition);
        float clearance = Mathf.Max(0.01f, _spawnClearance);

        // 2D 판정이므로 Z는 무시합니다. 경계가 맞닿는 후보도 제외합니다.
        return spawnBounds.max.x + clearance < playerBounds.min.x ||
            spawnBounds.min.x - clearance > playerBounds.max.x ||
            spawnBounds.max.y + clearance < playerBounds.min.y ||
            spawnBounds.min.y - clearance > playerBounds.max.y;
    }

    private Vector3 GetFloorPositionFromDoorSpawnPoint(Vector3 spawnPosition)
    {
        float playerBottomOffset = _player.transform.position.y - _playerCollider.bounds.min.y;
        spawnPosition.y -= playerBottomOffset;
        return spawnPosition;
    }

    public void Despawn()
    {
        // 잡힌 연출 중에 비활성화되면 코루틴이 끊겨 암전 후 복구되지 않습니다.
        if (_isInDeathSequence)
            return;

        _mapRoutes.Clear();
        _mapTransitionWaitRemaining = null;
        _currentMap = null;
        _lastPlayerMap = null;
        gameObject.SetActive(false);
        
        ResetFullScreenGlitch();
        State = ECharacterState.Unspawned;

        MapManager.Instance?.RefreshMapBGM();
    }

    public void SetMoveSpeed(float amount)
    {
        if (amount < 0) return;

        _speed = amount;
    }

    public void BeginPostSpawnWait()
    {
        if (State == ECharacterState.Unspawned) return;

        _postSpawnWaitRemaining = PostSpawnWaitSeconds;
        State = ECharacterState.WaitingAfterSpawn;
    }

    private void UpdateAnimation()
    {
        switch (_state)
        {
            case ECharacterState.Spawning:
                _animator.Play(SpawnAnimationName, 0, 0);
                break;
            case ECharacterState.Run:
                _animator.Play("Run");
                break;
        }
    }

    private void LateUpdate()
    {
        // 메뉴가 게임 시간을 멈춰도 화면 글리치의 패턴은 계속 움직입니다.
        if (_fullScreenGlitchMaterial != null)
            _fullScreenGlitchMaterial.SetFloat(GlitchTimeId, Time.unscaledTime);
    }

    private void ResetFullScreenGlitch()
    {
        if (_fullScreenGlitchMaterial == null) return;

        _fullScreenGlitchMaterial.SetFloat(IntensityId, 0f);
        _fullScreenGlitchMaterial.SetFloat(GlitchTimeId, 0f);
    }

    private void FixedUpdate()
    {
        if (IsGameplayPaused) return;

        bool isCutscenePlaying = CutsceneManager.Instance != null && CutsceneManager.Instance.IsCutscenePlaying;

        if (_state == ECharacterState.Spawning)
        {
            // Animator.Play 직후에는 이전 상태를 읽을 수 있으므로 변신 상태인지도 확인합니다.
            var animationState = _animator.GetCurrentAnimatorStateInfo(0);
            if (animationState.IsName(SpawnAnimationName) && animationState.normalizedTime >= 1f)
            {
                // 첫 조우 컷씬에서는 달리기 연출만 시작하고, 종료 후 0.2초 대기를 시작합니다.
                if (isCutscenePlaying)
                    _animator.Play("Run", 0, 0f);
                else
                    BeginPostSpawnWait();
            }
            return;
        }

        if (isCutscenePlaying) return;

        switch (_state)
        {
            case ECharacterState.Idle:
                break;
            case ECharacterState.WaitingAfterSpawn:
                // 메뉴·맵 이동·컷씬·대사 중에는 남은 대기 시간도 유지합니다.
                if (DialogSystem.Instance != null && DialogSystem.Instance.IsDialogPlaying) break;
                _postSpawnWaitRemaining = Mathf.Max(0f, _postSpawnWaitRemaining - Time.fixedDeltaTime);
                if (_postSpawnWaitRemaining <= 0f)
                    State = ECharacterState.Run;
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
            _mapTransitionWaitRemaining = null;
            _lastPlayerMap = playerMap;
            MoveHorizontallyTowards(_player.transform.position.x);
            return;
        }

        if (_lastPlayerMap != playerMap)
        {
            _lastPlayerMap = playerMap;
            _mapRoutes = MapManager.Instance.FindRoute(_currentMap, transform.position, playerMap, _player.transform.position);
            _mapTransitionWaitRemaining = null;
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
            _mapTransitionWaitRemaining = null;
            return;
        }

        Collider2D transitionArea = currentRoute.TransitionArea;
        bool hasTransitionArea = transitionArea != null;
        bool reachedTransition = hasTransitionArea && _collider.bounds.Intersects(transitionArea.bounds);

        float targetX = currentRoute.TransitionPoint.position.x;
        bool reachedPositionWithoutArea = hasTransitionArea == false && Mathf.Abs(targetX - transform.position.x) <= 0.01f;

        if (reachedTransition || reachedPositionWithoutArea)
        {
            // 이동 지점에 도착한 뒤 추격이 진행되는 게임 시간으로 1.5초 기다립니다.
            if (!_mapTransitionWaitRemaining.HasValue)
                _mapTransitionWaitRemaining = 1.5f;
            else
                _mapTransitionWaitRemaining -= Time.fixedDeltaTime;

            if (_mapTransitionWaitRemaining <= 0f)
            {
                _mapTransitionWaitRemaining = null;
                MoveThroughMapTransition();
            }
            return;
        }

        _mapTransitionWaitRemaining = null;
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
        SpriteRenderer.flipX = _direction < 0f;

        Vector3 targetPosition = new Vector3(targetX, transform.position.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, _speed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsGameplayPaused) return;

        if (State != ECharacterState.Run)
            return;

        // 세이브 로드(재시작) 중에는 잡기 연출이 시작되지 않게 합니다.
        if (SaveManager.Instance != null && SaveManager.Instance.IsLoading)
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
        if (IsGameplayPaused) return;

        if (State != ECharacterState.Run)
            return;

        // 세이브 로드(재시작) 중에는 잡기 연출이 시작되지 않게 합니다.
        if (SaveManager.Instance != null && SaveManager.Instance.IsLoading)
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

        _isInDeathSequence = true;

        IsChaseActive = false;
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
        _isInDeathSequence = false;
    }

    void OnEnable()
    {
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif

        if (CutsceneManager.Instance != null)
        {
            CutsceneManager.Instance.OnCutsceneStart += PauseKunekune;
            CutsceneManager.Instance.OnCutsceneEnd += ResumeKunekune;
        }
    }

    void OnDisable()
    {
        _mapTransitionWaitRemaining = null;
        _postSpawnWaitRemaining = 0f;
        // 공유 머티리얼의 런타임 값이 타이틀 등 다음 씬에 남지 않게 합니다.
        ResetFullScreenGlitch();
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif

        if (CutsceneManager.Instance != null)
        {
            CutsceneManager.Instance.OnCutsceneStart -= PauseKunekune;
            CutsceneManager.Instance.OnCutsceneEnd -= ResumeKunekune;
        }
    }

#if UNITY_EDITOR
    void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
            ResetFullScreenGlitch();
    }
#endif
}
