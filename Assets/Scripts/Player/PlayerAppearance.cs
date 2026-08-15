using UnityEngine;

public enum PlayerAge
{
    Adult,
    Student,
    Child
}

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

    void Awake()
    {
        if (anim == null) 
            anim = GetComponentInChildren<Animator>();
        if (bodyCollider == null) 
            bodyCollider = GetComponent<CapsuleCollider2D>();
        
        movement = GetComponent<PlayerMovement>();

        SetAge(PlayerAge.Adult);
    }

    void OnEnable()
    {
        if (movement != null) 
            movement.OnCrouchStateChanged += HandleCrouchStateChanged;
    }

    public void SetAge(PlayerAge age)
    {
        currentProfile = age switch
        {
            PlayerAge.Adult => adultProfile,
            PlayerAge.Student => studentProfile,
            PlayerAge.Child => childProfile,
            _ => null
        };

        if (currentProfile == null)
        {
            Debug.LogWarning($"No profile found for age: {age}", this);
            return;
        }

        ApplyAnimationProfile();
        ApplyColliderProfile();
    }

    private void HandleCrouchStateChanged(bool isCrouching)
    {
        ApplyColliderProfile();
    }

    private void ApplyAnimationProfile()
    {
        if (currentProfile.animatorOverride == null) return;

        anim.runtimeAnimatorController = currentProfile.animatorOverride;
    }

    private void ApplyColliderProfile()
    {
        if (currentProfile == null) return;

        if (movement.IsCrouching)
        {
            bodyCollider.size = currentProfile.crouchingColliderSize;
            bodyCollider.offset = currentProfile.crouchingColliderOffset;
            Debug.Log("Crouching Collider 적용 완료");
        }
        else
        {
            bodyCollider.size = currentProfile.standingColliderSize;
            bodyCollider.offset = currentProfile.standingColliderOffset;
            Debug.Log("Standing Collider 적용 완료");
        }
    }
}
