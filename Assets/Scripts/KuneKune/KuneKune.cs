using UnityEngine;
using System.Collections;

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
}
