using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 4f;
    private Vector2 moveDirection;
    public float CurrentFacingDirection { get; private set; } = 1f;

    [Header("Sprint Settings")]
    [SerializeField] private float sprintSpeed = 6f;
    private bool isSprintPressed = false;

    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaConsumeRate = 20f;
    [SerializeField] private float staminaRecoveryRate = 30f;
    public float CurrentStamina; //{ get; private set; }

    private Rigidbody2D rb;

    public event Action<float, float> OnStaminaChanged; // Invoke(currentStamina, maxStamina);

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        CurrentStamina = maxStamina;
    }

    void Update()
    {
        // 바라보는 방향 보정 (localScale.x 조작)
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x) * CurrentFacingDirection,
            transform.localScale.y,
            transform.localScale.z
        );

        // 스테미너 소모 & 회복 로직
        if (isSprintPressed && moveDirection != Vector2.zero)   // L-Shift 누른 채로 **실제로 움직일 때**만 소모
        {
            ConsumeStamina();
        }
        else
        {
            RecoverStamina();
        }
    }

    void FixedUpdate()
    {
        float speed = isSprintPressed ? sprintSpeed : moveSpeed;

        rb.linearVelocity = moveDirection * speed;
    }

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
        isSprintPressed = state;
    }

    private void RecoverStamina()
    {
        if (CurrentStamina >= maxStamina) return;

        CurrentStamina += staminaRecoveryRate * Time.deltaTime;
        OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
    }

    private void ConsumeStamina()
    {
        if (CurrentStamina <= 0f) return;

        CurrentStamina -= staminaConsumeRate * Time.deltaTime;
        OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
    }
}
