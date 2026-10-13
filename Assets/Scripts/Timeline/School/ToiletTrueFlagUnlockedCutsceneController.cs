using UnityEngine;
using UnityEngine.Timeline;

[RequireComponent(typeof(CutscenePlayerSwapper))]
public class ToiletTrueFlagUnlockedCutsceneController : CutsceneControllerBase
{
    private const string HallwayExitEntryID = "school_hallway_3";

    [SerializeField] private Transform _kuneKune;
    [SerializeField] private Transform _player;

    private ToiletTrueFlagCutsceneController _sourceCutscene;
    private CutscenePlayerSwapper _playerSwapper;

    public bool CanPlay => isActiveAndEnabled && gameObject.activeInHierarchy &&
        !IsCutsceneRunning && !(playOnlyOnce && hasPlayed) &&
        (SaveManager.Instance == null || !SaveManager.Instance.IsLoading) &&
        (CutsceneManager.Instance == null || !CutsceneManager.Instance.IsCutscenePlaying) &&
        (DialogSystem.Instance == null || !DialogSystem.Instance.IsDialogPlaying);

    protected override void Awake()
    {
        base.Awake();
        _playerSwapper = GetComponent<CutscenePlayerSwapper>();
        MapArea map = GetComponentInParent<MapArea>();
        if (map != null)
            _sourceCutscene = map.GetComponentInChildren<ToiletTrueFlagCutsceneController>(true);

        ResolveActors();
        if (_player != null)
            _player.gameObject.SetActive(false);
    }

    private void ResolveActors()
    {
        if (_kuneKune == null && _sourceCutscene != null)
            _kuneKune = _sourceCutscene.KuneKuneActor;
        if (_player == null && _kuneKune != null)
            _player = _kuneKune.Find("Player");
    }

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
        // 이벤트에서 멈춰 둔 Animator를 Timeline이 재생할 수 있게 복구합니다.
        _sourceCutscene?.ResumeKuneKuneAnimation();
    }

    protected override void OnCutsceneEndSetup()
    {
        _sourceCutscene?.PauseKuneKuneAnimation();

        // 배우의 좌표로 옮기지 않고 암전 중 실제 플레이어의 표시와 충돌만 복원합니다.
        _playerSwapper?.RestorePlayer();

        // 종료 페이드로 완전히 암전된 뒤 남자 화장실 문 앞의 복도 엔트리로 이동합니다.
        MapManager.Instance?.InitMap(EMapType.School_Hallway, HallwayExitEntryID);
    }

    protected override void OnDisable()
    {
        _playerSwapper?.RestorePlayer();
        _sourceCutscene?.PauseKuneKuneAnimation();
        base.OnDisable();
    }
}
