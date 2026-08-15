using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour
{
    private PlayerInputHandler input;
    private PlayerMovement movement;

    void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        movement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        movement.SetMoveDirection(input.MoveInput);
        movement.SetSprintState(input.IsSprintPressed);
        movement.SetCrawlState(input.IsCrawlPressed);
    }
}
