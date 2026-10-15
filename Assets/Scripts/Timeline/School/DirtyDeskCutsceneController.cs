using UnityEngine;

public class DirtyDeskCutsceneController : CutsceneControllerBase
{
    public const string CompletedFlag = "HasCompletedDirtyDeskCutscene";

    [SerializeField] private FootprintSequence footprintSequence;
    [SerializeField] private GameObject rewardItem;
    private PlayerLight playerLight;

    public bool HasCompleted { get; private set; }

    private void Start()
    {
        if (rewardItem != null) rewardItem.SetActive(false);
        playerLight = FindFirstObjectByType<PlayerLight>();
    }

    protected override void OnCutsceneStartSetup()
    {
        // 시작 암전이 완료된 뒤 기존 쿠네쿠네를 디스폰합니다.
        if (MapManager.Instance != null && MapManager.Instance.KuneKune != null)
            MapManager.Instance.KuneKune.Despawn();
    }

    protected override void OnCutsceneFinished()
    {
        HasCompleted = true;
        SaveManager.Instance?.SetFlag(CompletedFlag, true);
    }

    protected override void OnCutsceneEndSetup()
    {
        // 조작 잠금이 풀리기 전에 즉시 낮춰 종료 직후 최대 더위로 재소환되지 않게 합니다.
        PlayerMovement player = MapManager.Instance?.Player?.GetComponent<PlayerMovement>();
        if (player != null && player.CurrentHeat > 95f)
            player.SetCurrentHeat(95f, isLoad: true);
    }

    public void Flicker()
    {
        if (LightingEventManager.Instance == null) return;
        
        StartCoroutine(LightingEventManager.Instance.FlickerRoutine());
    }

    public void ResetScene()
    {
        if (LightingEventManager.Instance != null)
        {
            LightingEventManager.Instance.SetGlobalLightColor(Color.white);
            LightingEventManager.Instance.SetBlackout(1f, 0.5f);
        }
        if (footprintSequence != null) footprintSequence.HideFootprint();
        if (playerLight != null) playerLight.TurnOff();
        if (rewardItem != null) rewardItem.SetActive(true);
    }
}
