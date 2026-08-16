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
            if (input.IsInteractPressed) interaction.CurrentTarget?.OnInteractPressed();
            if (input.IsInteractHeld) interaction.CurrentTarget?.OnInteractHeld();
            if (input.IsInteractReleased) interaction.CurrentTarget?.OnInteractReleased();    
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
