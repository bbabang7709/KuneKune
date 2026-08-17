using System.Collections;
using UnityEditor;
using UnityEngine;

public enum ECharacterState
{
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

        State = ECharacterState.Run;

        Show();
    }

    private void UpdateAnimation()
    {
        switch (_state)
        {
            case ECharacterState.Idle:
                _animator.Play("Run");
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
            case ECharacterState.Idle:
                break;
            case ECharacterState.Run:
                ChasePlayer();
                break;
        }
    }

    private void Show()
    {
        gameObject.SetActive(true);
        _fullScreenGlitchMaterial.SetFloat(IntensityId, Mathf.Clamp01(intensity));  // 0(정상 화면) ~ 1(글리치 강도 최대)


    }

    public void Hide()
    {
        gameObject.SetActive(false);
        _fullScreenGlitchMaterial.SetFloat(IntensityId, 0f);    // 0(정상 화면)
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
