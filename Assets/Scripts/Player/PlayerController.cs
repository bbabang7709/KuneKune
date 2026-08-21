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
        if (movement != null)
        {
            movement.SetMoveDirection(input.MoveInput);
            movement.SetSprintState(input.IsSprintPressed);
            movement.SetCrouchState(input.IsCrouchPressed);
        }
        
        if (interaction != null)
        {
            // Hold 중에는 플레이어가 이동하지 못하도록 합니다.
            bool interactionBlockingMovement = interaction.IsHolding;

            if (!interactionBlockingMovement)
            {
                movement.SetMoveDirection(input.MoveInput);
                movement.SetSprintState(input.IsSprintPressed);
                movement.SetCrouchState(input.IsCrouchPressed);
            }

            if (!movement.IsCrouching)
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

        if (anim != null)
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
