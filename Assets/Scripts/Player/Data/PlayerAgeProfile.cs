using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerAgeProfile", menuName = "KuneKune/Age Profile")]
public class PlayerAgeProfile : ScriptableObject
{
    [Header("Animation")]
    public AnimatorOverrideController animatorOverride;
    
    [Header("Standing Collider")]
    public Vector2 standingColliderSize;
    public Vector2 standingColliderOffset;

    [Header("Crouching Collider")]
    public Vector2 crouchingColliderSize;
    public Vector2 crouchingColliderOffset;
}
