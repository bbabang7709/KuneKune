using System;
using UnityEngine;

/// <summary>
/// 플레이어의 이동, Sprint, Crouch 및 Stamina 상태를 관리한다.
/// Rigidbody2D를 이용하여 실제 플레이어 이동을 처리한다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    // =========================================================
    // Movement Settings
    // =========================================================

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 4f;

    private Vector2 moveDirection;

    /// <summary>
    /// 현재 플레이어가 바라보는 방향을 반환한다.
    /// 1은 오른쪽, -1은 왼쪽을 의미한다.
    /// </summary>
    public float CurrentFacingDirection { get; private set; } = 1f;

    /// <summary>
    /// 플레이어가 현재 이동 중인지 반환한다.
    /// </summary>
    public bool IsMoving { get; private set; }

    /// <summary>
    /// 플레이어의 이동이 잠겨 있는지 반환한다.
    /// </summary>
    public bool IsMovementLocked { get; private set; }

    // =========================================================
    // Sprint Settings
    // =========================================================

    [Header("Sprint Settings")]
    [SerializeField] private float sprintSpeed = 6f;

    [SerializeField] private float minStaminaToSprint = 20f;

    private bool isSprintPressed;
    private bool isSprintLocked;
    private bool isSprintMode;

    /// <summary>
    /// 실제 Sprint 속도가 적용되고 있는지 반환한다.
    /// </summary>
    public bool IsSprinting { get; private set; }

    // =========================================================
    // Crouch Settings
    // =========================================================

    [Header("Crouch Settings")]
    [SerializeField] private float crouchSpeed = 1.5f;

    private bool isCrouchPressed;

    /// <summary>
    /// 현재 Crouch 상태인지 반환한다.
    /// </summary>
    public bool IsCrouching { get; private set; }

    // =========================================================
    // Stamina Settings
    // =========================================================

    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;

    [SerializeField] private float staminaConsumeRate = 20f;

    [SerializeField] private float staminaRecoveryRate = 30f;

    [SerializeField] private float staminaRecoveryDelay = 1f;

    /// <summary>
    /// 현재 Stamina 값을 반환한다.
    /// </summary>
    public float CurrentStamina { get; private set; }

    // =========================================================
    // Exhaustion Settings
    // =========================================================
    [Header("Heat Settings")]
    [SerializeField] private bool _test = false;
    [SerializeField] private float maxHeat = 100f;
    [SerializeField] private float minHeat = 20f;   // 화장실
    [SerializeField] private float heatDecreaseRate = 20f;
    // 연령에 따라 다른 더위 게이지 증가량
    [SerializeField] private float adultHeatIncreaseRate = 10f;
    [SerializeField] private float studentHeatIncreaseRate = 20f;
    [SerializeField] private float childHeatIncreaseRate = 25f;
    public float CurrentHeat { get; private set; } = 0f;

    /// <summary>
    /// 현재 더위 게이지를 0부터 1 사이의 비율로 반환합니다.
    /// </summary>
    public float HeatRatio => maxHeat > 0f ? CurrentHeat / maxHeat : 0f;

    /// <summary>
    /// 햇빛에 노출되어 있는가?
    /// </summary>
    public bool IsExposedToSunlight { get; set; } = false;

    [Header("Exhaustion Settings")]
    [SerializeField] private float exhaustionDuration = 2f;

    /// <summary>
    /// 현재 탈진 상태인지 반환한다.
    /// </summary>
    public bool IsExhausted { get; private set; }

    private float recoveryDelayTimer;
    private float exhaustionTimer;

    private Rigidbody2D rb;
    private PlayerAppearance appearance;

    /// <summary>
    /// Stamina가 변경될 때 호출되는 이벤트.
    /// currentStamina, maxStamina 순서로 전달된다.
    /// </summary>
    public event Action<float, float> OnStaminaChanged;

    /// <summary>
    /// Crouch 상태가 변경될 때 호출되는 이벤트.
    /// 변경된 Crouch 상태를 전달한다.
    /// </summary>
    public event Action<float, float> OnHeatChanged;    // Invoke(CurrentHeat, maxHeat);
    public event Action<bool> OnCrouchStateChanged;

    // =========================================================
    // Unity Lifecycle
    // =========================================================

    /// <summary>
    /// Rigidbody2D를 가져오고 Stamina를 최대값으로 초기화한다.
    /// </summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        appearance = GetComponent<PlayerAppearance>();
        CurrentStamina = maxStamina;
        IsExposedToSunlight = false;
    }

    /// <summary>
    /// 플레이어의 상태를 매 프레임 갱신한다.
    /// </summary>
    private void Update()
    {
        UpdateFacingDirection();
        UpdateExhaustion();
        UpdateCrawlingState();
        UpdateSprintingState();
        UpdateStamina();
        UpdateHeat();

        IsMoving = rb.linearVelocity != Vector2.zero;
    }

    /// <summary>
    /// Rigidbody2D를 이용한 실제 이동을 처리한다.
    /// </summary>
    private void FixedUpdate()
    {
        // 이동 잠금 또는 탈진 상태에서는 이동하지 않는다.
        if (IsMovementLocked || IsExhausted)
        {
            rb.linearVelocity = Vector2.zero;
            IsMoving = false;
            return;
        }

        float speed = IsCrouching
            ? crouchSpeed
            : IsSprinting
                ? sprintSpeed
                : moveSpeed;

        rb.linearVelocity = moveDirection * speed;
    }

    // =========================================================
    // Movement Control
    // =========================================================

    /// <summary>
    /// 플레이어의 이동을 잠그거나 해제한다.
    /// 이동이 잠기는 순간 현재 이동 방향과 Rigidbody 속도를 초기화한다.
    /// </summary>
    /// <param name="locked">true면 이동을 잠그고 false면 이동을 허용한다.</param>
    public void SetMovementLocked(bool locked)
    {
        IsMovementLocked = locked;

        if (locked)
        {
            moveDirection = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            IsMoving = false;

            IsSprinting = false;
            isSprintMode = false;
        }
    }

    /// <summary>
    /// 현재 이동 입력을 기반으로 이동 방향과 바라보는 방향을 설정한다.
    /// </summary>
    /// <param name="input">이동 입력값.</param>
    public void SetMoveDirection(float input)
    {
        // 이동 잠금 상태에서는 입력을 받지 않는다.
        if (IsMovementLocked)
        {
            moveDirection = Vector2.zero;
            return;
        }

        if (input != 0f)
        {
            CurrentFacingDirection = input;
        }

        moveDirection = input > 0f
            ? Vector2.right
            : input < 0f
                ? Vector2.left
                : Vector2.zero;
    }

    // =========================================================
    // State & Logic
    // =========================================================

    /// <summary>
    /// 현재 바라보는 방향에 따라 플레이어 오브젝트의 localScale을 조정한다.
    /// </summary>
    private void UpdateFacingDirection()
    {
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x) * CurrentFacingDirection,
            transform.localScale.y,
            transform.localScale.z
        );
    }

    /// <summary>
    /// 탈진 상태를 갱신한다.
    /// </summary>
    private void UpdateExhaustion()
    {
        if (!IsExhausted)
            return;

        exhaustionTimer -= Time.deltaTime;

        if (exhaustionTimer <= 0f)
        {
            IsExhausted = false;
        }
    }

    /// <summary>
    /// Crouch 상태를 갱신한다.
    /// </summary>
    private void UpdateCrawlingState()
    {
        bool shouldCrouch =
            isCrouchPressed &&
            !IsExhausted &&
            !IsMovementLocked;

        if (IsCrouching != shouldCrouch)
        {
            IsCrouching = shouldCrouch;
            OnCrouchStateChanged?.Invoke(IsCrouching);
        }
    }

    /// <summary>
    /// Sprint 조건을 검사하고 Sprint 상태를 갱신한다.
    /// </summary>
    private void UpdateSprintingState()
    {
        if (IsMovementLocked)
        {
            isSprintMode = false;
            IsSprinting = false;
            return;
        }

        // Sprint 모드 해제 조건.
        if (!isSprintPressed ||
            isSprintLocked ||
            IsExhausted ||
            IsCrouching ||
            CurrentStamina <= 0f)
        {
            isSprintMode = false;
        }
        // 새로운 Sprint 모드 진입 조건.
        else if (!isSprintMode &&
                 CurrentStamina >= minStaminaToSprint)
        {
            isSprintMode = true;
        }

        IsSprinting =
            isSprintMode &&
            moveDirection != Vector2.zero;
    }

    /// <summary>
    /// Sprint 상태에 따라 Stamina를 소모하거나 회복한다.
    /// </summary>
    private void UpdateStamina()
    {
        if (IsSprinting)
        {
            ConsumeStamina();
            recoveryDelayTimer = staminaRecoveryDelay;
        }
        else
        {
            HandleStaminaRecovery();
        }
    }

    /// <summary>
    /// Stamina 회복 대기 시간과 실제 회복을 처리한다.
    /// </summary>
    private void HandleStaminaRecovery()
    {
        if (recoveryDelayTimer > 0f)
        {
            recoveryDelayTimer -= Time.deltaTime;
        }
        else
        {
            RecoverStamina();
        }
    }

    // =========================================================
    // Input State
    // =========================================================
    private void UpdateHeat()
    {
        // 테스트 중에는 현재 맵과 햇빛 노출 상태에 관계없이 더위를 올린다.
        if (_test)
        {
            IncreaseHeat();
            return;
        }

        if (MapManager.Instance?.CurrentMap == null)
            return;
        
        if (DialogSystem.Instance.IsDialogPlaying) return;  // 다이얼로그 연출 중에는 더위 게이지 갱신 중단

        // 화장실(세이프 존)
        // TODO: 아이템 사용(부채, 시원한 물) 인식
        if (MapManager.Instance.CurrentMap.mapType == EMapType.CoolZone)
            DecreaseHeat(minHeat);
        // 햇빛 노출될 때 더위 게이지 증가
        else if (IsExposedToSunlight)
            IncreaseHeat();
    }

    // --- Public API --- //

    /// <summary>
    /// Sprint 입력 상태를 설정한다.
    /// </summary>
    /// <param name="state">Sprint 입력 상태.</param>
    public void SetSprintState(bool state)
    {
        isSprintPressed = state;

        // Sprint 키를 놓으면 잠금을 해제한다.
        if (!state)
        {
            isSprintLocked = false;
        }
    }

    /// <summary>
    /// Crouch 입력 상태를 설정한다.
    /// </summary>
    /// <param name="state">Crouch 입력 상태.</param>
    public void SetCrouchState(bool state)
    {
        isCrouchPressed = state;
    }

    // =========================================================
    // Stamina
    // =========================================================

    /// <summary>
    /// Stamina를 회복한다.
    /// </summary>
    private void RecoverStamina()
    {
        CurrentStamina = Mathf.Min(
            maxStamina,
            CurrentStamina +
            staminaRecoveryRate * Time.deltaTime
        );

        OnStaminaChanged?.Invoke(
            CurrentStamina,
            maxStamina
        );
    }

    /// <summary>
    /// Sprint 중 Stamina를 소모한다.
    /// </summary>
    private void ConsumeStamina()
    {
        CurrentStamina = Mathf.Max(
            0f,
            CurrentStamina -
            staminaConsumeRate * Time.deltaTime
        );

        OnStaminaChanged?.Invoke(
            CurrentStamina,
            maxStamina
        );

        if (CurrentStamina <= 0f)
        {
            TriggerExhaustion();
        }
    }

    /// <summary>
    /// 플레이어를 탈진 상태로 전환한다.
    /// </summary>
    private void TriggerExhaustion()
    {
        IsExhausted = true;
        isSprintLocked = true;
        IsSprinting = false;

        exhaustionTimer = exhaustionDuration;
        recoveryDelayTimer = staminaRecoveryDelay;
    }
    
    private void IncreaseHeat()
    {
        float heatIncreaseRate = appearance.CurrentAge switch
        {
            EPlayerAge.Adult => adultHeatIncreaseRate,
            EPlayerAge.Student => studentHeatIncreaseRate,
            EPlayerAge.Child => childHeatIncreaseRate,
            _ => 0f
        };

        CurrentHeat = Mathf.Min(maxHeat, CurrentHeat + heatIncreaseRate * Time.deltaTime);
        OnHeatChanged?.Invoke(CurrentHeat, maxHeat);

        // 쿠네쿠네가 아직 소환되지 않은 상태
        KuneKune kuneKune = MapManager.Instance != null ? MapManager.Instance.KuneKune : null;
        if (kuneKune != null && kuneKune.State == ECharacterState.Unspawned && CurrentHeat >= maxHeat)
            // 더위 게이지 100 달성하면 쿠네쿠네가 등장한다
            kuneKune.Spawn();
    }

    private void DecreaseHeat(float min = 0f)
    {
        // 더위 게이지가 천천히 내려간다
        CurrentHeat = Mathf.Max(min, CurrentHeat - heatDecreaseRate * Time.deltaTime);
        OnHeatChanged?.Invoke(CurrentHeat, maxHeat);

        if (MapManager.Instance.KuneKune.State != ECharacterState.Unspawned)
            MapManager.Instance.KuneKune.Despawn();
    }
}
