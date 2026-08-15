using UnityEngine;

/// <summary>
/// 플레이어의 입력을 감지 및 raw data 저장
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    public PlayerInputActions Actions { get; private set; }
    public float MoveInput { get; private set; }    // 1D Axis
    public bool IsSprinting => Actions.Player.Sprint.IsPressed();

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
