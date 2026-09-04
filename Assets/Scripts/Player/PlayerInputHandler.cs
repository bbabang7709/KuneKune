using UnityEngine;

/// <summary>
/// 플레이어의 입력을 감지하고 각 시스템에서 사용할 수 있도록
/// 입력 상태를 제공한다.
/// Unity Input System을 기반으로 동작한다.
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    /// <summary>
    /// 생성된 Input Action의 인스턴스.
    /// </summary>
    public PlayerInputActions Actions { get; private set; }

    // =========================================================
    // Move
    // =========================================================

    /// <summary>
    /// 현재 이동 입력값을 반환한다.
    /// </summary>
    public float MoveInput { get; private set; }

    // =========================================================
    // Sprint
    // =========================================================

    /// <summary>
    /// Sprint 버튼이 현재 눌려 있는지 반환한다.
    /// </summary>
    public bool IsSprintPressed =>
        Actions.Player.Sprint.IsPressed();

    // =========================================================
    // Crouch
    // =========================================================

    /// <summary>
    /// Crawl 버튼이 현재 눌려 있는지 반환한다.
    /// </summary>
    public bool IsCrouchPressed =>
        Actions.Player.Crawl.IsPressed();

    // =========================================================
    // Interact
    // =========================================================

    /// <summary>
    /// 이번 프레임에 상호작용 버튼이 눌렸는지 반환한다.
    /// </summary>
    public bool IsInteractPressed =>
        Actions.Player.Interact.WasPressedThisFrame();

    /// <summary>
    /// 상호작용 버튼이 현재 눌려 있는지 반환한다.
    /// </summary>
    public bool IsInteractHeld =>
        Actions.Player.Interact.IsPressed();

    /// <summary>
    /// 이번 프레임에 상호작용 버튼을 놓았는지 반환한다.
    /// </summary>
    public bool IsInteractReleased =>
        Actions.Player.Interact.WasReleasedThisFrame();

    // =========================================================
    // Dialog
    // =========================================================

    /// <summary>
    /// 이번 프레임에 대화 진행 입력이 발생했는지 반환한다.
    /// F, Space, Mouse Left Button을 사용한다.
    /// </summary>
    public bool IsDialogAdvancePressed =>
        Actions.Player.DialogAdvance.WasPressedThisFrame();

    /// <summary>
    /// 대화 진행 입력이 현재 눌려 있는지 반환한다.
    /// </summary>
    public bool IsDialogAdvanceHeld =>
        Actions.Player.DialogAdvance.IsPressed();

    /// <summary>
    /// 이번 프레임에 대화 진행 입력을 놓았는지 반환한다.
    /// </summary>
    public bool IsDialogAdvanceReleased =>
        Actions.Player.DialogAdvance.WasReleasedThisFrame();
    
    // =========================================================
    // Pause
    // =========================================================
    public bool IsPausePressed => 
        Actions.Player.Pause.WasPressedThisFrame();
    // =========================================================
    // Unity Lifecycle
    // =========================================================

    /// <summary>
    /// Input Action을 생성한다.
    /// </summary>
    private void Awake()
    {
        Actions = new PlayerInputActions();
    }

    /// <summary>
    /// Input Action을 활성화한다.
    /// </summary>
    private void OnEnable()
    {
        Actions.Enable();
    }

    /// <summary>
    /// Input Action을 비활성화한다.
    /// </summary>
    private void OnDisable()
    {
        Actions.Disable();
    }

    /// <summary>
    /// 이동 입력을 갱신한다.
    /// </summary>
    private void Update()
    {
        MoveInput = Actions.Player.Move.ReadValue<float>();
    }
}