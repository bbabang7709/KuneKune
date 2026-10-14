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
    public float WalkingSpeed => moveSpeed;
    
    private float _speedMultiplier = 1f;
    private Vector2 moveDirection;
    private bool _isCutsceneMovementControlled;

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
    [SerializeField] private float adultHeatIncreaseRate = 4f;
    [SerializeField] private float studentHeatIncreaseRate = 20f;
    [SerializeField] private float childHeatIncreaseRate = 25f;

    [SerializeField] private float sweatThreshold = 50f; // 땀을 흘리기 시작할 더위

    [Header("Max Heat Stamina Recovery")]
    [Tooltip("최대 더위가 이 시간 동안 유지된 후 스태미나 회복 속도가 감소합니다.")]
    [Min(0f)]
    [SerializeField] private float maxHeatRecoveryPenaltyDelay = 5f;
    [Tooltip("회복 속도가 최소 배율에 도달하기까지 걸리는 시간입니다.")]
    [Min(0.01f)]
    [SerializeField] private float maxHeatRecoveryPenaltyDuration = 15f;
    [Tooltip("최대 더위가 오래 지속될 때 적용할 스태미나 회복 속도의 최소 배율입니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float minStaminaRecoveryMultiplierAtMaxHeat = 0.25f;

    private float maxHeatDuration;
    private float staminaRecoveryMultiplier = 1f;

    public float CurrentHeat { get; private set; } = 0f;
    public float MaxHeat => maxHeat;
    private float? targetHeat;
    private float? _heatIncreaseRateOverride;

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

    private float _leftMoveLimitX = float.MinValue;

    public void SetLeftMovementLimit(float limitX)
    {
        _leftMoveLimitX = limitX;
    }

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
        UpdateHeat();
        UpdateMaxHeatRecoveryPenalty();
        UpdateStamina();

        IsMoving = rb.linearVelocity != Vector2.zero;
    }

    /// <summary>
    /// Rigidbody2D를 이용한 실제 이동을 처리한다.
    /// </summary>
    private void FixedUpdate()
    {
        // 컷씬에서 MovePosition으로 이동할 때 일반 이동 코드가 속도를 덮어쓰지 않게 합니다.
        if (_isCutsceneMovementControlled)
            return;

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

        rb.linearVelocity = moveDirection * speed * _speedMultiplier;
        ClampLeftMovementLimit();
    }

    /// <summary>
    /// 왼쪽 이동 한계선을 넘지 않도록 Rigidbody 위치와 속도를 보정한다.
    /// Transform을 직접 옮기면 보간/카메라 추적이 튀므로 물리 스텝에서 처리한다.
    /// </summary>
    private void ClampLeftMovementLimit()
    {
        if (rb.position.x < _leftMoveLimitX)
            rb.position = new Vector2(_leftMoveLimitX, rb.position.y);

        if (rb.position.x <= _leftMoveLimitX && rb.linearVelocity.x < 0f)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
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

    /// <summary>
    /// 이동 입력을 바꾸지 않고 바라보는 방향만 설정합니다. 컷씬 중에도 적용됩니다.
    /// </summary>
    public void SetFacingDirection(float direction)
    {
        if (direction == 0f)
            return;

        CurrentFacingDirection = Mathf.Sign(direction);
        UpdateFacingDirection();
    }

    public void SetCutsceneMovementControlled(bool controlled)
    {
        StopImmediately();
        _isCutsceneMovementControlled = controlled;
    }

    /// <summary>
    /// 조작이 잠기는 순간 이동 방향과 속도를 즉시 끊는다.
    /// 다음 Update를 기다리면 그 사이의 FixedUpdate가 이전 방향으로 한 번 더 이동시킨다.
    /// </summary>
    public void StopImmediately()
    {
        moveDirection = Vector2.zero;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        IsMoving = false;
        IsSprinting = false;
        isSprintMode = false;
    }

    public void SetSpeedMultiplier(float amount)
    {
        if (amount < 0) return;
        _speedMultiplier = amount;
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
    /// <summary>
    /// 최대 더위의 연속 유지 시간에 따라 스태미나 회복 속도를 점진적으로 낮춘다.
    /// 더위가 최대값 아래로 내려가면 즉시 정상 회복 속도로 되돌린다.
    /// </summary>
    private void UpdateMaxHeatRecoveryPenalty()
    {
        if (maxHeat <= 0f || CurrentHeat < maxHeat)
        {
            maxHeatDuration = 0f;
            staminaRecoveryMultiplier = 1f;
            return;
        }

        // 더위 갱신이 멈추는 대화 및 조작 잠금 중에는 유지 시간을 누적하지 않는다.
        if (IsMovementLocked ||
            (DialogSystem.Instance != null && DialogSystem.Instance.IsDialogPlaying) ||
            (ControlLockManager.Instance != null &&
             (ControlLockManager.Instance.IsMovementLocked || ControlLockManager.Instance.IsInputLocked)))
            return;

        float delay = Mathf.Max(0f, maxHeatRecoveryPenaltyDelay);
        float duration = Mathf.Max(0.01f, maxHeatRecoveryPenaltyDuration);
        maxHeatDuration = Mathf.Min(maxHeatDuration + Time.deltaTime, delay + duration);

        float progress = Mathf.Clamp01((maxHeatDuration - delay) / duration);
        staminaRecoveryMultiplier = Mathf.Lerp(1f, Mathf.Clamp01(minStaminaRecoveryMultiplierAtMaxHeat), progress);
    }

    private void UpdateHeat()
    {
        // 임의 증감 요청은 환경에 따른 갱신보다 먼저 목표값까지 처리합니다.
        if (targetHeat.HasValue)
        {
            if (DialogSystem.Instance != null && DialogSystem.Instance.IsDialogPlaying)
                return;

            float target = targetHeat.Value;
            if (CurrentHeat < target)
                IncreaseHeat(target);
            else if (CurrentHeat > target)
                DecreaseHeat(target);

            if (CurrentHeat == target)
                targetHeat = null;
            return;
        }

        // 테스트 중에는 현재 맵과 햇빛 노출 상태에 관계없이 더위를 올린다.
        if (_test)
        {
            IncreaseHeat();
            return;
        }

        if (MapManager.Instance?.CurrentMap == null)
            return;
        
        if (DialogSystem.Instance.IsDialogPlaying) return;  // 다이얼로그 연출 중에는 더위 게이지 갱신 중단

        // 컷씬 후 활성화된 햇빛 영역은 화장실의 기본 냉각보다 우선한다.
        if (IsExposedToSunlight)
            IncreaseHeat();

        // 햇빛에 노출되지 않은 화장실에서는 기존대로 더위를 낮춘다.
        // TODO: 아이템 사용(부채, 시원한 물) 인식
        else if (MapManager.Instance.IsCoolZone(MapManager.Instance.CurrentMapType))
            DecreaseHeat(minHeat);
        
        bool shouldSweat = CurrentHeat >= sweatThreshold;
        appearance.SetSweatyState(shouldSweat);
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
            staminaRecoveryRate * staminaRecoveryMultiplier * Time.deltaTime
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
    
    private void IncreaseHeat(float? limit = null)
    {
        if (ControlLockManager.Instance != null && (ControlLockManager.Instance.IsMovementLocked || ControlLockManager.Instance.IsInputLocked))
            return;

        float heatIncreaseRate = _heatIncreaseRateOverride ?? (appearance.CurrentAge switch
        {
            EPlayerAge.Adult => adultHeatIncreaseRate,
            EPlayerAge.Student => studentHeatIncreaseRate,
            EPlayerAge.Child => childHeatIncreaseRate,
            _ => 0f
        });

        CurrentHeat = Mathf.Min(limit ?? maxHeat, CurrentHeat + heatIncreaseRate * Time.deltaTime);
        OnHeatChanged?.Invoke(CurrentHeat, maxHeat);

        // 쿠네쿠네가 아직 소환되지 않은 상태
        KuneKune kuneKune = MapManager.Instance != null ? MapManager.Instance.KuneKune : null;
        if (kuneKune != null && kuneKune.State == ECharacterState.Unspawned && CurrentHeat >= maxHeat)
            // 더위 게이지 100 달성하면 쿠네쿠네가 등장한다
            kuneKune.Spawn();
    }

    private void DecreaseHeat(float min = 0f)
    {
        if (ControlLockManager.Instance != null && (ControlLockManager.Instance.IsMovementLocked || ControlLockManager.Instance.IsInputLocked))
            return;
        // 더위 게이지가 천천히 내려간다
        // 복구 목표가 안전 구역 최저값보다 낮아도 감소 처리로 다시 올리지 않습니다.
        CurrentHeat = Mathf.Max(Mathf.Min(min, CurrentHeat), CurrentHeat - heatDecreaseRate * Time.deltaTime);
        OnHeatChanged?.Invoke(CurrentHeat, maxHeat);

        KuneKune kuneKune = MapManager.Instance != null ? MapManager.Instance.KuneKune : null;
        if (kuneKune != null && kuneKune.State != ECharacterState.Unspawned)
            kuneKune.Despawn();
    }

    /// <summary>
    /// 이벤트 중 더위 증가 속도를 임시 적용하고, null이면 연령별 기본 속도로 복구합니다.
    /// </summary>
    public void SetHeatIncreaseRateOverride(float? rate)
    {
        _heatIncreaseRateOverride = rate.HasValue ? Mathf.Max(0f, rate.Value) : null;
    }

    /// <summary>
    /// 로드 시에는 즉시 설정하고, 일반 호출은 amount만큼 증감할 목표를 설정합니다.
    /// 증가에는 연령별 증가율, 감소에는 heatDecreaseRate를 사용합니다.
    /// </summary>
    public void SetCurrentHeat(float amount, bool isLoad = false)
    {
        if (amount < 0f || amount > maxHeat) return;
        
        if (isLoad)
        {
            targetHeat = null;
            CurrentHeat = amount;
            maxHeatDuration = 0f;
            staminaRecoveryMultiplier = 1f;
            OnHeatChanged?.Invoke(CurrentHeat, maxHeat);
            return;
        }

        // 새 요청은 현재 값을 기준으로 목표를 바꾸며, 값 자체는 UpdateHeat에서 갱신합니다.
        targetHeat = Mathf.Clamp(amount, 0f, maxHeat);
    }
}
