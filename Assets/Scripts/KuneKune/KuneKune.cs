using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public enum ECharacterState
{
    Spawning,   // 쿠네쿠네가 사물에서 나타나는 상태
    Idle,
    Run,
}

public class KuneKune : MonoBehaviour
{
    private Animator _animator;
    private Collider2D _collider;
    private PlayerController _player;

    private ECharacterState _state = ECharacterState.Idle;
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

    private float _speed = 10f;

    private string _spawnAnimationName = "";

    // TODO: 맵 전환 시 업데이트하도록 구현 필요
    // 원래 여기 말고 맵 관련 코드에 있어야 하지만.. 일단 임시로 둠
    public Transform KuneKune_ables;    // ex. house_bedroom의 KuneKune_ables

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
        _animator = GetComponent<Animator>();
        _collider = GetComponent<Collider2D>();
        _player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();

        // 플레이어와 충돌 방지
        int playerLayerMask = LayerMask.GetMask("Player");
        _collider.excludeLayers = playerLayerMask;
        
        _state = ECharacterState.Idle;
        Spawn();    // TODO: test용이다. 제거 필요.
    }

    /// <summary>
    /// 정적 함수입니다. 쿠네쿠네 프리팹을 인스턴스화하고, KuneKune 컴포넌트를 반환합니다.
    /// 쿠네쿠네를 현재 씬에서 처음 인스턴스화 할 때 사용하세요.
    /// </summary>
    /// <returns></returns>
    public static KuneKune Instantiate()
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/Character/KuneKune");
        return Instantiate(prefab).GetComponent<KuneKune>();
    }

    /// <summary>
    /// 플레이어와 가까운 사물에 쿠네쿠네를 소환합니다.
    /// </summary>
    public void Spawn()
    {
        #region 플레이어랑 가까운 사물 찾기
        Transform[] children = KuneKune_ables.GetComponentsInChildren<Transform>();
        
        GameObject closest = null;
        float closestDistance = Mathf.Infinity;
        
        foreach (Transform child in children)
        {
            float distance = Vector3.Distance(_player.transform.position, child.transform.position);
            if (child.CompareTag("KunekuneProp") && distance < closestDistance)
            {
                closestDistance = distance;
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

        // 쿠네쿠네 위치를 사물이 놓인 바닥 높이에 맞도록 올린다
        //SpriteRenderer closestRenderer = closest.GetComponent<SpriteRenderer>();
        //float floorY = closestRenderer != null ? closestRenderer.bounds.min.y : closest.transform.position.y;
        //float offset = transform.position.y - _collider.bounds.min.y;
        //transform.position = new Vector3(closest.transform.position.x, floorY + offset, closest.transform.position.z);

        transform.position = closest.transform.position;
        gameObject.SetActive(true);
        closest.SetActive(false);   // 쿠네쿠네가 활성화되면 사물은 비활성화

        _fullScreenGlitchMaterial.SetFloat(IntensityId, Mathf.Clamp01(intensity));  // 0(정상 화면) ~ 1(글리치 강도 최대)

        _spawnAnimationName = Regex.Replace(closest.name, @"_0(?: \(\d+\))?$", "");
        State = ECharacterState.Spawning;
    }

    /// <summary>
    /// 소환한 쿠네쿠네를 비활성화하고, 글리치 효과를 끕니다.
    /// </summary>
    public void Despawn()
    {
        gameObject.SetActive(false);
        _fullScreenGlitchMaterial.SetFloat(IntensityId, 0f);    // 0(정상 화면)
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
                {
                    State = ECharacterState.Run;
                }
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
        if (distance < 1.2f)
        {
            State = ECharacterState.Idle;
            return;
        }

        Vector3 direction = (_player.transform.position - transform.position).normalized;
        direction = (direction.x > 0) ? Vector3.right : Vector3.left;
        transform.position += direction * _speed * Time.fixedDeltaTime;
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
