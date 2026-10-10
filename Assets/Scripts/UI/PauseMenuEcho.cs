using UnityEngine;

/// <summary>
/// 메뉴의 쿠네쿠네 활성화 조건을 재사용 가능한 CanvasEcho에 연결합니다.
/// </summary>
public class PauseMenuEcho : MonoBehaviour
{
    [SerializeField] private CanvasEcho canvasEcho;
    private PlayerMovement _player;

    private bool CanShowEcho()
    {
        return isActiveAndEnabled && PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPaused &&
            _player != null && _player.CurrentHeat >= _player.MaxHeat &&
            (SaveManager.Instance == null || !SaveManager.Instance.IsLoading);
    }

    public void Play()
    {
        Stop();
        _player = MapManager.Instance?.Player?.GetComponent<PlayerMovement>();
        if (CanShowEcho())
            canvasEcho?.Play();
    }

    public void Stop()
    {
        canvasEcho?.Stop();
    }

    private void Update()
    {
        if (canvasEcho != null && canvasEcho.IsPlaying && !CanShowEcho())
            Stop();
    }

    private void OnDisable()
    {
        Stop();
    }
}
