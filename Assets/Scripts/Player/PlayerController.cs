using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerAnimation))]
public class PlayerController : MonoBehaviour
{
    private PlayerInputHandler input;
    private PlayerMovement movement;
    private PlayerAnimation anim;
    private PlayerAppearance appearance;

    void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        movement = GetComponent<PlayerMovement>();
        anim = GetComponent<PlayerAnimation>();
        appearance = GetComponent<PlayerAppearance>();
    }

    void Update()
    {
        movement.SetMoveDirection(input.MoveInput);
        movement.SetSprintState(input.IsSprintPressed);
        movement.SetCrouchState(input.IsCrouchPressed);

        anim.UpdateAnimation(
            movement.IsMoving,
            movement.IsSprinting,
            movement.IsCrouching,
            movement.IsExhausted
        );
    }

    public void SetAge(EPlayerAge age)
    {
        appearance.SetAge(age);
    }
}
