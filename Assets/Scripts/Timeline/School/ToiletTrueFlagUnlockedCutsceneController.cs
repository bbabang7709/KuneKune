using System.Collections;
using UnityEngine;
using UnityEngine.Timeline;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CutscenePlayerSwapper))]
[RequireComponent(typeof(DialogTimelineController))]
public class ToiletTrueFlagUnlockedCutsceneController : CutsceneControllerBase
{
    [SerializeField] private Transform _kuneKune;
    [SerializeField] private Transform _player;
    [SerializeField] private DialogData openingDialog;

    [Header("시그널 암전")]
    [SerializeField, Min(0f)] private float fadeOutDuration = 2f;

    private ToiletTrueFlagCutsceneController _sourceCutscene;
    private CutscenePlayerSwapper _playerSwapper;
    private DialogTimelineController _dialogController;
    private bool _openingDialogFinished;
    private float? _playerHeightBeforeCutscene;

    public bool CanPlay => isActiveAndEnabled && gameObject.activeInHierarchy &&
        !IsCutsceneRunning && !(playOnlyOnce && hasPlayed) &&
        (SaveManager.Instance == null || !SaveManager.Instance.IsLoading) &&
        (CutsceneManager.Instance == null || !CutsceneManager.Instance.IsCutscenePlaying) &&
        (DialogSystem.Instance == null || !DialogSystem.Instance.IsDialogPlaying);

    protected override void Awake()
    {
        base.Awake();
        _playerSwapper = GetComponent<CutscenePlayerSwapper>();
        _dialogController = GetComponent<DialogTimelineController>();
        MapArea map = GetComponentInParent<MapArea>();
        if (map != null)
            _sourceCutscene = map.GetComponentInChildren<ToiletTrueFlagCutsceneController>(true);

        ResolveActors();
        if (_player != null)
            _player.gameObject.SetActive(false);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _dialogController.DialogFinished += OnDialogFinished;
    }

    private void OnDialogFinished(DialogData dialog)
    {
        if (IsCutsceneRunning && openingDialog != null && dialog == openingDialog)
            _openingDialogFinished = true;
    }

    private void ResolveActors()
    {
        if (_kuneKune == null && _sourceCutscene != null)
            _kuneKune = _sourceCutscene.KuneKuneActor;
        if (_player == null && _kuneKune != null)
            _player = _kuneKune.Find("Player");
    }

#if UNITY_EDITOR
    private void LateUpdate()
    {
        if (Keyboard.current == null || !Keyboard.current.tKey.wasPressedThisFrame ||
            IsCutsceneRunning || Time.timeScale <= 0f ||
            (SaveManager.Instance != null && SaveManager.Instance.IsLoading) ||
            (CutsceneManager.Instance != null && CutsceneManager.Instance.IsCutscenePlaying) ||
            (DialogSystem.Instance != null && DialogSystem.Instance.IsDialogPlaying) ||
            (ControlLockManager.Instance != null &&
                (ControlLockManager.Instance.IsInputLocked || ControlLockManager.Instance.IsMovementLocked)))
            return;

        // 에디터에서는 선행 이벤트와 1회 재생 제한 없이 컷씬을 확인합니다.
        ResolveActors();
        if (!BindActors()) return;

        HoldControls();
        _sourceCutscene?.StopKuneKune();
        _sourceCutscene?.PauseKuneKuneAnimation();
        _sourceCutscene?.CompleteTrueFlagInteraction();
        _kuneKune.gameObject.SetActive(true);
        hasPlayed = false;
        PlayCutscene();
    }
#endif

    public bool TryPlay(ToiletTrueFlagCutsceneController sourceCutscene)
    {
        if (!CanPlay || sourceCutscene == null || !sourceCutscene.CanInteractWithKuneKune)
            return false;

        _sourceCutscene = sourceCutscene;
        ResolveActors();
        if (!BindActors())
            return false;

        // 조작 잠금과 성공 처리를 같은 프레임에 수행해 더위 게임 오버와 중복 상호작용을 막습니다.
        HoldControls();
        _sourceCutscene.StopKuneKune();
        _sourceCutscene.PauseKuneKuneAnimation();
        _sourceCutscene.CompleteTrueFlagInteraction();
        PlayCutscene();
        return IsCutsceneRunning;
    }

    private bool BindActors()
    {
        Animator playerAnimator = _player != null ? _player.GetComponent<Animator>() : null;
        Animator kuneKuneAnimator = _kuneKune != null ? _kuneKune.GetComponent<Animator>() : null;
        if (director.playableAsset is not TimelineAsset timeline || playerAnimator == null ||
            kuneKuneAnimator == null || _playerSwapper == null)
        {
            Debug.LogError("닦아주는 컷씬의 타임라인과 플레이어·쿠네쿠네 배우를 지정해주세요.", this);
            return false;
        }

        bool hasPlayerTrack = false;
        bool hasKuneKuneTrack = false;
        foreach (TrackAsset track in timeline.GetOutputTracks())
        {
            if (track is not AnimationTrack) continue;

            foreach (TimelineClip clip in track.GetClips())
            {
                if (clip.asset is not AnimationPlayableAsset animation || animation.clip == null)
                    continue;

                if (animation.clip.name == "PC_ToiletTrueFlag")
                {
                    director.SetGenericBinding(track, playerAnimator);
                    hasPlayerTrack = true;
                }
                else if (animation.clip.name == "Cutscene_ToiletTrueFlag_Idle")
                {
                    director.SetGenericBinding(track, kuneKuneAnimator);
                    hasKuneKuneTrack = true;
                }
            }
        }

        if (!hasPlayerTrack || !hasKuneKuneTrack)
        {
            Debug.LogError("닦아주는 컷씬의 플레이어와 쿠네쿠네 애니메이션 트랙을 확인해주세요.", this);
            return false;
        }

        _playerSwapper.SetDummyPlayer(_player.gameObject,
            restorePositionFromDummy: false, restoreOnDirectorStopped: false);
        return true;
    }

    protected override void OnCutsceneStartSetup()
    {
        _openingDialogFinished = false;
        _playerHeightBeforeCutscene = MapManager.Instance?.Player?.transform.position.y;
        // 이벤트에서 멈춰 둔 Animator를 Timeline이 재생할 수 있게 복구합니다.
        _sourceCutscene?.ResumeKuneKuneAnimation();
    }

    protected override IEnumerator WaitBeforeCutsceneReveal()
    {
        if (!useFadeInOut || UIManager.Instance == null || openingDialog == null)
        {
            yield return base.WaitBeforeCutsceneReveal();
            yield break;
        }

        // 시작 암전을 유지한 채 손수건 대사를 화면 앞에 표시합니다.
        yield return UIManager.Instance.FadeOutKeepingDialog(0f);
        yield return new WaitUntil(() => _openingDialogFinished || !IsCutsceneRunning ||
            (SaveManager.Instance != null && SaveManager.Instance.IsLoading));
        // 지정한 대사가 끝난 뒤 기본 시작 루틴이 화면을 페이드 인합니다.
    }

    /// <summary>Timeline 시그널에서 호출하여 화면을 다시 암전시킵니다.</summary>
    public void FadeOut()
    {
        if (!isActiveAndEnabled || UIManager.Instance == null) return;

        StartCoroutine(FadeOutAndStopGlitch());
    }

    private IEnumerator FadeOutAndStopGlitch()
    {
        yield return UIManager.Instance?.FadeOutKeepingDialog(Mathf.Max(0f, fadeOutDuration));
        _sourceCutscene?.StopToiletGlitch();
    }

    protected override void OnCutsceneEndSetup()
    {
        // 타임라인 암전 시그널이 생략되어도 종료 암전 중에 글리치를 정리합니다.
        _sourceCutscene?.StopToiletGlitch();
        UIManager.Instance?.RestoreDialogFadeOrder();
        _sourceCutscene?.PauseKuneKuneAnimation();

        // 충돌체 복원을 새 진입으로 처리하지 않도록 먼저 진입 트리거를 억제합니다.
        MapArea currentMap = MapManager.Instance?.CurrentMap;
        if (currentMap != null)
        {
            foreach (ToiletCutsceneController entryCutscene in
                currentMap.GetComponentsInChildren<ToiletCutsceneController>(true))
                entryCutscene.SuppressEntryUntilExit();
        }

        // 배우의 X좌표만 사용하고 실제 플레이어의 시작 높이와 Z좌표를 유지합니다.
        GameObject realPlayer = MapManager.Instance?.Player;
        realPlayer?.GetComponent<PlayerMovement>()?.StopImmediately();
        if (realPlayer != null && _player != null && _playerHeightBeforeCutscene.HasValue)
        {
            Vector3 position = realPlayer.transform.position;
            position.x = _player.position.x;
            position.y = _playerHeightBeforeCutscene.Value;
            realPlayer.transform.position = position;
            Rigidbody2D body = realPlayer.GetComponent<Rigidbody2D>();
            if (body != null) body.position = new Vector2(position.x, position.y);
        }

        // 충돌체를 켜기 전에 좌표를 맞추며, 배우용 높이 오프셋은 적용하지 않습니다.
        if (_player != null)
            _playerSwapper?.SetDummyPlayer(_player.gameObject,
                restorePositionFromDummy: false, restoreOnDirectorStopped: false);
        _playerSwapper?.RestorePlayer();
        _playerHeightBeforeCutscene = null;
        Physics2D.SyncTransforms();

        // 화장실에 남은 상태에서 쿠네쿠네와 이벤트 전용 더위 영역을 정리합니다.
        _sourceCutscene?.HideKuneKune();
        _kuneKune?.gameObject.SetActive(false);
    }

    protected override void OnCutsceneFinished()
    {
        SaveManager saveManager = SaveManager.Instance;
        MapArea currentMap = MapManager.Instance?.CurrentMap;
        GameObject realPlayer = MapManager.Instance?.Player;
        if (saveManager == null || !saveManager.GetFlag(SaveManager.TrueEndingUnlockedFlag) ||
            currentMap == null || realPlayer == null || currentMap.mapType != EMapType.School_ManToilet ||
            currentMap.GetEntryPoint(ToiletMirror.CheckpointEntryID) == null)
            return;

        // 배우·글리치 정리와 실제 플레이어 복원, 화면 페이드가 모두 끝난 뒤 진행도를 저장합니다.
        Vector3 localPosition = currentMap.transform.InverseTransformPoint(realPlayer.transform.position);
        saveManager.SaveMapCheckpoint(EMapType.School_ManToilet, ToiletMirror.CheckpointEntryID, localPosition);
    }

    protected override void OnDisable()
    {
        if (IsCutsceneRunning)
            _sourceCutscene?.StopToiletGlitch();
        if (_dialogController != null)
            _dialogController.DialogFinished -= OnDialogFinished;
        _openingDialogFinished = false;
        _playerHeightBeforeCutscene = null;
        UIManager.Instance?.RestoreDialogFadeOrder();
        _playerSwapper?.RestorePlayer();
        _sourceCutscene?.PauseKuneKuneAnimation();
        base.OnDisable();
    }
}
