using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// UI에 배치해 둔 공포 이미지를 화면 아래에서 올려 보여준 뒤 한 번에 숨긴다.
/// 연출 동안 플레이어 이동과 입력을 잠그며, 에디터에서 배치한 위치가 최종 노출 위치가 된다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class RisingScareImage : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float riseDuration = 0.25f;
    [SerializeField] private float holdDuration = 2f;

    [Header("Position")]
    [Tooltip("이미지가 최종적으로 보이는 anchoredPosition. 시작 위치는 화면 아래 바깥으로 자동 계산한다.")]
    [SerializeField] private Vector2 shownPosition = Vector2.zero;

    [Header("Sound (optional)")]
    [SerializeField] private string sfxName = "KuneKune_Jumpscare";

    private RectTransform _rect;
    private bool _isPlaying;

    private void Awake()
    {
        _rect = (RectTransform)transform;
    }

    private void Start()
    {
        // Awake에서 끄면 Play()의 첫 SetActive(true) 직후 다시 꺼지므로 Start에서 숨긴다.
        if (!_isPlaying) gameObject.SetActive(false);
    }

    public void Play(Action onFinished = null)
    {
        if (_isPlaying) return;

        if (_rect == null) _rect = (RectTransform)transform;

        gameObject.SetActive(true);
        StartCoroutine(ScareRoutine(onFinished));
    }

    private IEnumerator ScareRoutine(Action onFinished)
    {
        _isPlaying = true;

        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.AddMovementLock(this);
            ControlLockManager.Instance.AddInputLock(this);
        }

        // 이미지가 화면 밖으로 완전히 나가도록 부모 높이 + 자기 높이만큼 아래에서 시작한다.
        float parentHeight = (_rect.parent as RectTransform)?.rect.height ?? Screen.height;
        Vector2 hiddenPosition = shownPosition + Vector2.down * (parentHeight + _rect.rect.height);

        _rect.anchoredPosition = hiddenPosition;

        if (!string.IsNullOrEmpty(sfxName))
        {
            SoundManager.Instance?.PlaySFX(sfxName);
        }

        float elapsed = 0f;
        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / riseDuration);
            // 끝으로 갈수록 급격히 치고 올라오는 느낌을 위해 ease-out.
            float eased = 1f - (1f - t) * (1f - t);
            _rect.anchoredPosition = Vector2.LerpUnclamped(hiddenPosition, shownPosition, eased);
            yield return null;
        }

        _rect.anchoredPosition = shownPosition;

        yield return new WaitForSeconds(holdDuration);

        _rect.anchoredPosition = shownPosition;
        gameObject.SetActive(false);

        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.RemoveMovementLock(this);
            ControlLockManager.Instance.RemoveInputLock(this);
        }

        _isPlaying = false;
        onFinished?.Invoke();
    }

    private void OnDisable()
    {
        // 연출 도중 비활성화돼도 잠금이 남지 않도록 보장한다.
        if (!_isPlaying) return;

        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.RemoveMovementLock(this);
            ControlLockManager.Instance.RemoveInputLock(this);
        }

        _isPlaying = false;
    }
}
