using UnityEngine;

/// <summary>
/// 플레이어의 나이를 표현하는 Enum Class  
/// (Adult, Student, Child)
/// </summary>
public enum EPlayerAge
{
    Adult,
    Student,
    Child
}

/// <summary>
/// Player의 외형(Sprite Animation / CapsuleCollider2D)을 관리하는 Class
/// </summary>
public class PlayerAppearance : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator anim;
    [SerializeField] private CapsuleCollider2D bodyCollider;

    [Header("Age Profiles")]
    [SerializeField] private PlayerAgeProfile adultProfile;
    [SerializeField] private PlayerAgeProfile studentProfile;
    [SerializeField] private PlayerAgeProfile childProfile;

    private PlayerMovement movement;
    private PlayerAgeProfile currentProfile;

    private bool _isSweaty = false;

    public EPlayerAge CurrentAge { get; private set; } = EPlayerAge.Adult;

    void Awake()
    {
        if (anim == null) 
            anim = GetComponentInChildren<Animator>();
        if (bodyCollider == null) 
            bodyCollider = GetComponent<CapsuleCollider2D>();
        
        movement = GetComponent<PlayerMovement>();
    }

    void Start()
    {
        SetAge(EPlayerAge.Adult);   // 시작은 어른
    }

    void OnEnable()
    {
        if (movement != null) 
            movement.OnCrouchStateChanged += HandleCrouchStateChanged;
    }

    void OnDisable()
    {
        if (movement != null) 
            movement.OnCrouchStateChanged -= HandleCrouchStateChanged;
    }

    /// <summary>
    /// 플레이어의 나이를 설정하는 메서드.
    /// </summary>
    /// <param name="age">
    /// PlayerAge.Adult, PlayerAge.Student, PlayerAge.Child
    /// </param>
    public void SetAge(EPlayerAge age)
    {
        currentProfile = age switch
        {
            EPlayerAge.Adult => adultProfile,
            EPlayerAge.Student => studentProfile,
            EPlayerAge.Child => childProfile,
            _ => null
        };

        if (currentProfile == null)
        {
            Debug.LogWarning($"No profile found for age: {age}", this);
            return;
        }

        CurrentAge = age;
        ApplyAnimationProfile();
    }

    public void SetSweatyState(bool isSweaty)
    {
        if (_isSweaty == isSweaty) return;

        _isSweaty = isSweaty;
        ApplyAnimationProfile();
    }

    private void HandleCrouchStateChanged(bool isCrouching)
    {
        ApplyColliderProfile(isCrouching);
    }

    // Animation 적용 메서드
    private void ApplyAnimationProfile()
    {
        if (currentProfile == null) return;

        AnimatorOverrideController targetController = _isSweaty && currentProfile.sweatyAnimatorOverride != null
            ? currentProfile.sweatyAnimatorOverride
            : currentProfile.animatorOverride;
            
        if (targetController != null && anim.runtimeAnimatorController != targetController)
        {
            anim.runtimeAnimatorController = targetController;
        }
    }

    // Collider 적용 메서드
    private void ApplyColliderProfile(bool isCrouching)
    {
        if (currentProfile == null) return;

        if (isCrouching)
        {
            bodyCollider.size = currentProfile.crouchingColliderSize;
            bodyCollider.offset = currentProfile.crouchingColliderOffset;
        }
        else
        {
            bodyCollider.size = currentProfile.standingColliderSize;
            bodyCollider.offset = currentProfile.standingColliderOffset;
        }
    }

    /// <summary>
    /// 콜라이더를 껐다 켜서 OnTriggerEnter2D를 강제 발생시킵니다.
    /// </summary>
    public void RefreshCollider()
    {
        if (bodyCollider != null)
        {
            bodyCollider.enabled = false;
            bodyCollider.enabled = true;
        }
    }
}
