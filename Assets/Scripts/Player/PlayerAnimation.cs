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
    private bool _isCutsceneWalking;
    private bool _isCutsceneSprinting;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public void UpdateAnimation(bool isMoving, bool isSprinting, bool isCrawling, bool isExhausted)
    {
        if (animator == null) return;
        if (_isCutsceneWalking || _isCutsceneSprinting)
        {
            isMoving = true;
            isSprinting = _isCutsceneSprinting;
            isCrawling = false;
            isExhausted = false;
        }
        animator.SetBool(IsMoving, isMoving);
        animator.SetBool(IsSprinting, isSprinting);
        animator.SetBool(IsCrouching, isCrawling);
        animator.SetBool(IsExhausted, isExhausted);
    }

    /// <summary>위치로 이동시키는 컷씬에서도 걷기 상태와 발소리 이벤트를 유지합니다.</summary>
    public void SetCutsceneWalking(bool isWalking)
    {
        if (_isCutsceneWalking == isWalking) return;
        _isCutsceneWalking = isWalking;
        if (animator == null) return;
        UpdateAnimation(isWalking, false, false, false);
        // 학생 프로필의 PC_Walk 상태는 PC_Student_Walk 클립을 재생합니다.
        animator.Play(isWalking ? "PC_Walk" : "PC_Idle", 0, 0f);
    }

    /// <summary>입력이 잠긴 컷씬에서도 달리기 애니메이션을 유지합니다.</summary>
    public void SetCutsceneSprinting(bool isSprinting)
    {
        if (_isCutsceneSprinting == isSprinting) return;
        _isCutsceneSprinting = isSprinting;
        if (isSprinting) _isCutsceneWalking = false;
        if (animator == null) return;
        UpdateAnimation(isSprinting, isSprinting, false, false);
        animator.Play(isSprinting ? "PC_Sprint" : "PC_Idle", 0, 0f);
    }

    public void PlayFootstep()
    {
        if (Time.time - _lastFootstepTime < footstepCooldown) return;
        _lastFootstepTime = Time.time;

        bool isIndoors = MapManager.Instance?.CurrentMap?.IsInside ?? false;
        string targetSoundID = _isCutsceneWalking || isIndoors ? insideFootstepID : outsideFootstepID;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayFootstepSFX(targetSoundID);
        }
    }
}
