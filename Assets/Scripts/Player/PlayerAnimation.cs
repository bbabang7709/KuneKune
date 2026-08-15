using UnityEngine;

/// <summary>
/// 플레이어 Animator의 Parameter에 값을 전달하는 Class
/// </summary>
public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;

    private static readonly int IsMoving = Animator.StringToHash("isMoving");
    private static readonly int IsSprinting = Animator.StringToHash("isSprinting");
    private static readonly int IsCrouching = Animator.StringToHash("isCrouching");
    private static readonly int IsExhausted = Animator.StringToHash("isExhausted");

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public void UpdateAnimation(bool isMoving, bool isSprinting, bool isCrawling, bool isExhausted)
    {
        animator.SetBool(IsMoving, isMoving);
        animator.SetBool(IsSprinting, isSprinting);
        animator.SetBool(IsCrouching, isCrawling);
        animator.SetBool(IsExhausted, isExhausted);
    }
}
