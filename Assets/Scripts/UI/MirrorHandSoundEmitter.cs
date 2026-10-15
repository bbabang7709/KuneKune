using UnityEngine;

/// <summary>거울에 손자국이 찍히는 애니메이션 이벤트에서 효과음을 재생합니다.</summary>
public class MirrorHandSoundEmitter : MonoBehaviour
{
    public void PlayMirrorHand(float volume)
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f) return;

        SoundManager.Instance?.PlaySFX("mirrorHand", Mathf.Clamp01(volume));
    }
}
