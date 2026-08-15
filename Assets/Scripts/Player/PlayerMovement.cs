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
    private bool isSprinting = false;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // 바라보는 방향 보정 (localScale.x 조작)
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x) * CurrentFacingDirection,
            transform.localScale.y,
            transform.localScale.z
        );
    }

    void FixedUpdate()
    {
        float speed = isSprinting ? sprintSpeed : moveSpeed;

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
        isSprinting = state;
    }
}
