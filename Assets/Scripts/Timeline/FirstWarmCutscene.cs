using System.Collections;
using UnityEngine;

public class FirstWarmCutscene : CutsceneControllerBase
{
    private const float HeatThreshold = 40f;
    private const string PlayedFlag = "FirstWarmCutscene_Played";

    private PlayerMovement _playerMovement;

    protected override void Awake()
    {
        base.Awake();
        director.playOnAwake = false;
        playOnlyOnce = true;
        _playerMovement = GetComponentInParent<PlayerMovement>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        StartCoroutine(StartMonitoringHeat());
    }

    private IEnumerator StartMonitoringHeat()
    {
        // 다른 오브젝트의 초기화와 저장 데이터 복원이 끝난 뒤 최초 상태를 확인합니다.
        yield return null;
        yield return new WaitUntil(() => SaveManager.Instance == null || !SaveManager.Instance.IsLoading);

        hasPlayed = hasPlayed || (SaveManager.Instance != null && SaveManager.Instance.GetFlag(PlayedFlag));
        if (hasPlayed) yield break;

        if (_playerMovement == null)
            _playerMovement = MapManager.Instance?.Player?.GetComponent<PlayerMovement>();

        if (_playerMovement == null)
        {
            Debug.LogError("[FirstWarmCutscene] 플레이어의 PlayerMovement를 찾을 수 없습니다.", this);
            yield break;
        }

        _playerMovement.OnHeatChanged += OnHeatChanged;
        OnHeatChanged(_playerMovement.CurrentHeat, _playerMovement.MaxHeat);
    }

    private void OnHeatChanged(float currentHeat, float maxHeat)
    {
        if (currentHeat < HeatThreshold || hasPlayed ||
            (SaveManager.Instance != null && SaveManager.Instance.IsLoading))
            return;

        PlayCutscene();
    }

    public override void PlayCutscene()
    {
        if (hasPlayed) return;

        base.PlayCutscene();
        if (!hasPlayed) return;

        SaveManager.Instance?.SetFlag(PlayedFlag, true);
        if (_playerMovement != null)
            _playerMovement.OnHeatChanged -= OnHeatChanged;
    }

    protected override void OnDisable()
    {
        if (_playerMovement != null)
            _playerMovement.OnHeatChanged -= OnHeatChanged;

        bool wasRunning = IsCutsceneRunning;
        base.OnDisable();
        if (wasRunning) CutsceneManager.Instance?.EndCutscene();
    }
}
