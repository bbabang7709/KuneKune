using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerController : MonoBehaviour
{
    private PlayerInputHandler input;
    private PlayerMovement movement;
    private PlayerAnimation anim;
    private PlayerAppearance appearance;
    private PlayerInteraction interaction;

    void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        movement = GetComponent<PlayerMovement>();
        anim = GetComponent<PlayerAnimation>();
        appearance = GetComponent<PlayerAppearance>();
        interaction = GetComponent<PlayerInteraction>();
    }

    void Update()
    {
        bool isInputLocked = ControlLockManager.Instance != null && ControlLockManager.Instance.IsInputLocked;
        bool isMovementLocked = ControlLockManager.Instance != null && ControlLockManager.Instance.IsMovementLocked;
        bool isInteractionBlocking = interaction != null && interaction.IsHolding;

        if (movement != null)
        {
            if (isInputLocked || isMovementLocked || isInteractionBlocking)
            {
                movement.SetMoveDirection(0);
                movement.SetSprintState(false);
                movement.SetCrouchState(false);
            }
            else
            {
                movement.SetMoveDirection(input.MoveInput);
                movement.SetSprintState(input.IsSprintPressed);
                movement.SetCrouchState(input.IsCrouchPressed);
            }
        }
        
        if (interaction != null)
        {
            // 입력이 잠기지 않았을 때만 상호작용
            if (!isInputLocked)
            {
                // 웅크리고 있지 않을 때만 상호작용
                if (movement != null && !movement.IsCrouching)
                {
                    if (input.IsInteractPressed)
                    {
                        interaction.StartInteraction();
                    }

                    if (input.IsInteractHeld)
                    {
                        interaction.UpdateHoldInteraction();
                    }

                    if (input.IsInteractReleased)
                    {
                        interaction.ReleaseInteraction();
                    }
                }
                else
                {
                    interaction.CancelInteraction();
                }
            }
        }

        if (anim != null && movement != null)
        {
            anim.UpdateAnimation(
                movement.IsMoving,
                movement.IsSprinting,
                movement.IsCrouching,
                movement.IsExhausted
            );
        }
    }

    public void SetAge(EPlayerAge age)
    {
        appearance.SetAge(age);
    }
}
