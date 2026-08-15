using UnityEngine;

/// <summary>
/// 플레이어의 나이에 따른 애니메이션 / 콜라이더 설정값을 가지는 Scriptable Object
/// </summary>
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
