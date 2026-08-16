using UnityEngine;

/// <summary>
/// 플레이어의 입력 감지 및 raw data 저장
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    public PlayerInputActions Actions { get; private set; }
    
    // --- Move --- //
    public float MoveInput { get; private set; }    // 1D Axis

    // --- Sprint --- //
    public bool IsSprintPressed => Actions.Player.Sprint.IsPressed();

    // --- Crouch --- //
    public bool IsCrouchPressed => Actions.Player.Crawl.IsPressed();

    // --- Interact --- //
    public bool IsInteractPressed => Actions.Player.Interact.WasPressedThisFrame();
    public bool IsInteractHeld => Actions.Player.Interact.IsPressed();
    public bool IsInteractReleased => Actions.Player.Interact.WasReleasedThisFrame();

    void Awake()
    {
        Actions = new PlayerInputActions();
    }

    void OnEnable() => Actions.Enable();
    void OnDisable() => Actions.Disable();

    void Update()
    {
        MoveInput = Actions.Player.Move.ReadValue<float>(); // 이동(A/D) 입력 감지 및 입력값 저장
    }
}
