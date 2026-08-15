using System;
using NUnit.Framework;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 4f;
    private Vector2 moveDirection;
    public float CurrentFacingDirection { get; private set; } = 1f; // 1f : right / -1f : left

    [Header("Sprint Settings")]
    [SerializeField] private float sprintSpeed = 6f;
    [SerializeField] private float minStaminaToSprint = 20f;    // 탈진 후 Sprint에 필요한 최소 스테미너
    private bool isSprintPressed = false;   
    private bool isSprintLocked = false;    // 탈진 시 Sprint를 강제로 막는 플래그
    private bool isSprintMode = false;      // Sprint 권한/모드 유지 플래그
    private bool isSprinting = false;       // 실제 Sprint 속도 적용 플래그

    [Header("Crawl Settings")]
    [SerializeField] private float crawlSpeed = 1.5f;
    [SerializeField] private Vector2 crawlColliderSize = new Vector2(0.8f, 0.5f);
    [SerializeField] private Vector2 crawlColliderOffset = new Vector2(0f, -0.25f);
    private bool isCrawlPressed = false;
    public bool IsCrawling { get; private set; } = false;

    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaConsumeRate = 20f;
    [SerializeField] private float staminaRecoveryRate = 30f;
    [SerializeField] private float staminaRecoveryDelay = 1f;   // Sprint 멈춤 후 회복 대기 시간
    public float CurrentStamina { get; private set; }

    [Header("Exhaustion Settings")]
    [SerializeField] private float exhaustionDuration = 2f;
    public bool IsExhausted { get; private set; } = false; 

    // TODO: 추후 Interact 구현 시 필요한 외부 참조용 프로퍼티 
    //public bool CanInteract => !IsCrawling && !IsExhausted;

    private float recoveryDelayTimer;
    private float exhaustionTimer;
    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider;

    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;

    public event Action<float, float> OnStaminaChanged; // Invoke(currentStamina, maxStamina);

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();

        CurrentStamina = maxStamina;

        originalColliderSize = capsuleCollider.size;
        originalColliderOffset = capsuleCollider.offset;
    }

    void Update()
    {
        UpdateFacingDirection();
        UpdateExhaustion();
        UpdateCrawlingState();
        UpdateSprintingState();
        UpdateStamina();
    }

    void FixedUpdate()
    {
        if (IsExhausted)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float speed = IsCrawling ? crawlSpeed
                    : isSprinting ? sprintSpeed 
                    : moveSpeed;

        rb.linearVelocity = moveDirection * speed;
    }

    // --- State & Logic Handlers --- //
    

    /// <summary>
    /// 현재 바라보는 방향으로 오브젝트의 localScale를 조정하는 메서드
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
    /// Exhaustion 상태를 갱신하는 메서드
    /// </summary>
    private void UpdateExhaustion()
    {
        if (!IsExhausted) return;

        exhaustionTimer -= Time.deltaTime;
        if (exhaustionTimer <= 0f)
        {
            IsExhausted = false;
        }
    }

    private void UpdateCrawlingState()
    {
        bool shouldCrawl = isCrawlPressed && !IsExhausted;

        if (IsCrawling != shouldCrawl)
        {
            IsCrawling = shouldCrawl;
            ApplyColliderState(IsCrawling);
        }
    }

    private void ApplyColliderState(bool crawling)
    {
        if (crawling)
        {
            capsuleCollider.size = crawlColliderSize;
            capsuleCollider.offset = crawlColliderOffset;
        }
        else
        {
            capsuleCollider.size = originalColliderSize;
            capsuleCollider.offset = originalColliderOffset;
        }
    }

    /// <summary>
    /// Sprint 조건을 체크하며 isSprinting 변수 갱신
    /// </summary>
    private void UpdateSprintingState()
    {   
        // Sprint 모드 해제 조건 (Shift 뗌 / 잠금 / 탈진 / 스테미너 고갈)
        if (!isSprintPressed || isSprintLocked || IsExhausted || IsCrawling || CurrentStamina <= 0f)
        {
            isSprintMode = false;
        }
        // 새 Sprint 모드 진입 조건 (모드가 꺼져있을 때에만 minStaminaToSprint 검사)
        else if (!isSprintMode && CurrentStamina >= minStaminaToSprint)
        {
            isSprintMode = true;
        }
        
        // Sprint 모드 유지 + 실제 이동 키가 눌려있을 때에만 isSprinting 활성화
        isSprinting = isSprintMode && (moveDirection != Vector2.zero);
    }

    /// <summary>
    /// Sprint 등 조건을 체크하여 Stamina 소모 또는 회복
    /// </summary>
    private void UpdateStamina()
    {
        if (isSprinting)
        {
            ConsumeStamina();
            recoveryDelayTimer = staminaRecoveryDelay;
        }
        else
        {
            HandleStaminaRecovery();
        }
    }

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

    // --- Public API --- //

    public void SetMoveDirection(float input)
    {
        // 현재 바라보는 방향 설정
        if (input != 0f) CurrentFacingDirection = input;

        // 이동 방향 설정
        moveDirection = input > 0 ? Vector2.right 
                      : input < 0 ? Vector2.left
                      : Vector2.zero;
    }

    public void SetSprintState(bool state)
    {
        if (isSprintPressed != state) isSprintPressed = state;

        // L-Shift 키를 떼는 순간 Sprint 다시 가능하도록 잠금 해제
        if (!state)
        {
            isSprintLocked = false;
        }
    }

    public void SetCrawlState(bool state)
    {
        if (isCrawlPressed != state) isCrawlPressed = state;
    }

    // --- Private Utilities --- //

    private void RecoverStamina()
    {
        CurrentStamina = Mathf.Min(maxStamina, CurrentStamina + staminaRecoveryRate * Time.deltaTime);
        OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
    }

    private void ConsumeStamina()
    {
        CurrentStamina = Mathf.Max(0f, CurrentStamina - staminaConsumeRate * Time.deltaTime); 
        OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
        
        if (CurrentStamina <= 0f)
        {
            TriggerExhaution();
        }
    }

    private void TriggerExhaution()
    {
        IsExhausted = true;
        isSprintLocked = true;
        isSprinting = false;
        exhaustionTimer = exhaustionDuration;
        recoveryDelayTimer = staminaRecoveryDelay;  // 회복 대기 카운트 시작
    }
}
