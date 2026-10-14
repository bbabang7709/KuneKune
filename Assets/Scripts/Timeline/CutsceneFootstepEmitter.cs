using UnityEngine;

/// <summary>컷씬 배우의 걷기 애니메이션 이벤트에서 발소리를 재생합니다.</summary>
public class CutsceneFootstepEmitter : MonoBehaviour
{
    [SerializeField] private string soundID = "Footstep_Inside";
    [Min(0f)] [SerializeField] private float cooldown = 0.2f;

    private AudioSource _source;
    private float _lastStepTime = float.NegativeInfinity;

    public void PlayFootstep()
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f || Time.time - _lastStepTime < cooldown || SoundManager.Instance == null)
            return;

        if (_source == null) _source = SoundManager.Instance.CreateSFXSource(transform);
        if (SoundManager.Instance.PlaySFXOnSource(_source, soundID))
            _lastStepTime = Time.time;
    }

    private void OnDisable()
    {
        if (_source != null) _source.Stop();
        _lastStepTime = float.NegativeInfinity;
    }
}
