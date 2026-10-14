using UnityEngine;

/// <summary>
/// 버스가 이동하는 동안에만 이동 소리를 재생합니다.
/// 버스 오브젝트에 붙이면 애니메이션으로 위치가 바뀔 때 자동으로 소리가 켜지고 멈추면 꺼집니다.
/// 직접 제어하려면 Auto Detect를 끄고 애니메이션 이벤트에서 StartMove/StopMove를 호출합니다.
/// </summary>
public class BusSound : MonoBehaviour
{
    [Tooltip("SoundDatabase에 등록한 버스 이동 효과음 ID")]
    [SerializeField] private string moveLoopID = "Bus_Move";

    [Header("Auto Detect")]
    [Tooltip("켜면 오브젝트 위치 변화로 이동 여부를 판단합니다.")]
    [SerializeField] private bool autoDetect = true;
    [Tooltip("이 속도(유닛/초)보다 빠르면 이동 중으로 봅니다.")]
    [SerializeField] private float moveSpeedThreshold = 0.05f;
    [Tooltip("멈춘 뒤 소리를 끄기까지의 유예 시간(초). 짧은 정지로 소리가 끊기지 않게 합니다.")]
    [SerializeField] private float stopDelay = 0.15f;

    private AudioSource _source;
    private Vector3 _lastPosition;
    private float _stillTime;

    // 애니메이션 이벤트 / Signal에서 호출
    public void StartMove()
    {
        if (_source != null || string.IsNullOrEmpty(moveLoopID) || SoundManager.Instance == null) return;

        _source = SoundManager.Instance.PlayLoopSFX(moveLoopID);
        SoundManager.Instance.SetLoopSFX(_source, 1f);
    }

    public void StopMove()
    {
        if (_source == null) return;

        SoundManager.Instance?.StopLoopSFX(_source);
        _source = null;
    }

    private void OnEnable()
    {
        _lastPosition = transform.position;
        _stillTime = 0f;
    }

    private void Update()
    {
        if (!autoDetect) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        float speed = (transform.position - _lastPosition).magnitude / dt;
        _lastPosition = transform.position;

        if (speed > moveSpeedThreshold)
        {
            _stillTime = 0f;
            StartMove();
        }
        else
        {
            _stillTime += dt;
            if (_stillTime >= stopDelay)
                StopMove();
        }
    }

    private void OnDisable()
    {
        StopMove();
    }
}
