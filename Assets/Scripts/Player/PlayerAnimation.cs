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
    
    [Header("Footstep Sounds")]
    [Tooltip("실외 발걸음 Sound ID")]
    [SerializeField] private string outsideFootstepID = "Footstep_Outside";
    [Tooltip("실내 발걸음 Sound ID")]
    [SerializeField] private string insideFootstepID = "Footstep_Inside";

    [Tooltip("발걸음 소리가 겹치지 않게 하는 최소 간격")]
    [SerializeField] private float footstepCooldown = 0.2f;
    private float _lastFootstepTime = 0f;

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

    public void PlayFootstep()
    {
        if (Time.time - _lastFootstepTime < footstepCooldown) return;
        _lastFootstepTime = Time.time;

        bool isIndoors = MapManager.Instance != null && MapManager.Instance.CurrentMap.IsInside;
        string targetSoundID = isIndoors ? insideFootstepID : outsideFootstepID;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayFootstepSFX(targetSoundID);
        }
    }
}
