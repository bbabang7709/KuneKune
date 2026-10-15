using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerController : MonoBehaviour
{
    private PlayerInputHandler input;
    private PlayerMovement movement;
    private PlayerAnimation anim;
    private PlayerAppearance appearance;
    private PlayerInteraction interaction;
    private ControlLockManager _lockManager;

    void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        movement = GetComponent<PlayerMovement>();
        anim = GetComponentInChildren<PlayerAnimation>();
        appearance = GetComponent<PlayerAppearance>();
        interaction = GetComponent<PlayerInteraction>();
    }

    private void Start()
    {
        // ControlLockManager는 Awake에서 등록되므로 Start에서 구독합니다.
        _lockManager = ControlLockManager.Instance;
        if (_lockManager != null)
            _lockManager.OnLockAdded += HandleLockAdded;
    }

    private void OnDestroy()
    {
        if (_lockManager != null)
            _lockManager.OnLockAdded -= HandleLockAdded;
    }

    /// <summary>
    /// 잠금이 걸린 프레임에 이미 설정된 이동 방향과 진행 중인 상호작용을 바로 끊습니다.
    /// </summary>
    private void HandleLockAdded()
    {
        movement?.StopImmediately();
        interaction?.CancelInteraction();
    }

    void Update()
    {
        // 1. 현재 Lock 상태 파악
        bool isInputLocked = ControlLockManager.Instance != null && ControlLockManager.Instance.IsInputLocked;
        bool isMovementLocked = ControlLockManager.Instance != null && ControlLockManager.Instance.IsMovementLocked;
        bool isInteractionBlocking = interaction != null && interaction.IsHolding;

        if (input.IsCancelPressed)
        {
            UIManager.Instance?.HandleCancelInput();
        }

        // 2. 이동 제어
        if (movement != null)
        {
            // 입력이 잠겼거나, 이동이 잠겼거나, 상호작용 중이라서 이동할 수 없는 경우
            if (isInputLocked || isMovementLocked || isInteractionBlocking)
            {
                // 💡 핵심 수정: 이동 상태를 강제로 0으로 초기화하여 미끄러짐 방지
                movement.SetMoveDirection(0f);
                movement.SetCrouchState(false);
            }
            else
            {
                // 정상적인 이동 처리
                movement.SetMoveDirection(input.MoveInput);
                movement.SetSprintState(input.IsSprintPressed);
                movement.SetCrouchState(input.IsCrouchPressed);
            }
        }

        // 3. 상호작용 제어
        if (interaction != null)
        {
            // 💡 입력이 잠기지 않았을 때만 상호작용 업데이트
            if (!isInputLocked)
            {
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
            else
            {
                // 컷씬 등으로 인해 갑자기 입력이 잠기면 진행 중이던 상호작용 강제 취소
                interaction.CancelInteraction();
            }
        }

        // 4. 애니메이션 제어
        // 💡 Lock이 걸려서 return으로 빠져나가면 걷는 애니메이션이 고정될 수 있으므로,
        // 애니메이션 업데이트는 Lock 여부와 상관없이 매 프레임 실행되어야 합니다.
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