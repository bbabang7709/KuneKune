using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class HouseCutsceneController : CutsceneControllerBase
{
    private bool _isWaitingForCutscene;

    private const int CutsceneCameraPriority = 20;

    [Header("첫 조우 컷씬 카메라")]
    [SerializeField] private CinemachineCamera _cutsceneCamera;
    [SerializeField] private CinemachineConfiner2D _bound;

    private PrioritySettings _cameraPriorityBeforeCutscene;
    /// <summary>
    /// 컷씬 카메라가 우선순위 설정되었는지 확인
    /// </summary>
    private bool _hasCameraPriorityOverride;
    /// <summary>
    /// 컷씬용 부드러운 전환 설정되었는지 확인
    /// </summary>
    private bool _isCameraBlendOverrideRegistered;
    private float _cameraBlendDuration = 2f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player") || (playOnlyOnce && hasPlayed))
            return;

        PlayerMovement playerMovement = collision.GetComponentInParent<PlayerMovement>();
        if (playerMovement == null) return;

        // 더위가 최대일 때는 화장실로 피신할 수 있게 두고, 재생 가능한 진입에서는 즉시 잠급니다.
        // 이미 대기 중이어도 화장실에서 복도로 돌아온 순간에는 잠금이 필요합니다.
        if (MapManager.Instance != null && MapManager.Instance.CurrentMapType == EMapType.House_Hallway &&
            playerMovement.CurrentHeat < playerMovement.MaxHeat)
            HoldControls();

        if (_isWaitingForCutscene)
            return;

        _isWaitingForCutscene = true;
        StartCoroutine(WaitAndPlayCutscene(playerMovement));
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _isWaitingForCutscene = false;
        ReleaseCutsceneCamera();
    }

    protected override void OnCutsceneEndSetup()
    {
        RestoreCameraPriority();
        //ReleaseCutsceneCamera();
        SaveManager.Instance?.SaveChapterCheckpoint(EChapter.Chapter2_House);
        QuestManager.Instance?.StartQuest("HouseCleanup");
    }

    private IEnumerator WaitAndPlayCutscene(PlayerMovement playerMovement)
    {
        // 더위가 최대라면 재생 요청을 유지합니다. 현관 트리거를 다시 밟을 필요는 없습니다.
        // 화장실에서 더위를 낮춘 뒤 복도로 돌아와 맵 전환과 다른 대사가 끝나면 재생합니다.
        yield return new WaitUntil(() =>
            (SaveManager.Instance == null || !SaveManager.Instance.IsLoading) &&
            MapManager.Instance != null && MapManager.Instance.CurrentMapType == EMapType.House_Hallway &&
            Time.timeScale > 0f &&
            playerMovement != null && playerMovement.CurrentHeat < playerMovement.MaxHeat &&
            (CutsceneManager.Instance == null || !CutsceneManager.Instance.IsCutscenePlaying) &&
            (DialogSystem.Instance == null || !DialogSystem.Instance.IsDialogPlaying));

        // 로드가 잠금을 지웠을 수 있으므로 다시 잠근 뒤 컷씬 잠금으로 넘깁니다.
        HoldControls();
        _isWaitingForCutscene = false;
        PlayCutscene();
    }

    #region 카메라 세팅
    /// <summary>
    /// Timeline Signal에서 호출합니다. duration초 동안 현재 카메라에서 CM으로 이동합니다.
    /// </summary>
    public void BlendToCutsceneCamera(float duration)
    {
        if (_cutsceneCamera == null)
        {
            Debug.LogWarning("첫 조우 컷씬의 CM 카메라를 지정해주세요.", this);
            return;
        }

        _cameraBlendDuration = Mathf.Max(0f, duration);
        SetCameraBound();

        if (!_isCameraBlendOverrideRegistered)
        {
            CinemachineCore.GetBlendOverride += GetCameraBlend;
            _isCameraBlendOverrideRegistered = true;
        }

        if (!_hasCameraPriorityOverride)
        {
            _cameraPriorityBeforeCutscene = _cutsceneCamera.Priority;
            _hasCameraPriorityOverride = true;
        }

        _cutsceneCamera.gameObject.SetActive(true);
        _cutsceneCamera.enabled = true;
        _cutsceneCamera.PreviousStateIsValid = false;
        _cutsceneCamera.Priority = CutsceneCameraPriority;
    }

    private void SetCameraBound()
    {
        if (_bound == null)
            _bound = _cutsceneCamera.GetComponent<CinemachineConfiner2D>();

        if (_bound != null && _bound.BoundingShape2D != null)
            return;

        if (CameraManager.Instance == null || CameraManager.Instance.CurrentCamera == null)
            return;

        if (CameraManager.Instance.CurrentCamera == _cutsceneCamera)
            return;

        var playerConfiner = CameraManager.Instance.CurrentCamera.GetComponent<CinemachineConfiner2D>();
        if (playerConfiner == null || playerConfiner.BoundingShape2D == null)
            return;

        _bound.BoundingShape2D = playerConfiner.BoundingShape2D;
        _bound.InvalidateBoundingShapeCache();
    }

    private CinemachineBlendDefinition GetCameraBlend(
        ICinemachineCamera fromCamera, ICinemachineCamera toCamera,
        CinemachineBlendDefinition defaultBlend, UnityEngine.Object owner)
    {
        // 다른 맵 카메라의 기본 전환 설정은 유지합니다.
        if (_cutsceneCamera == null ||
            (!ReferenceEquals(fromCamera, _cutsceneCamera) && !ReferenceEquals(toCamera, _cutsceneCamera)))
            return defaultBlend;

        return new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, _cameraBlendDuration);
    }

    private void ReleaseCutsceneCamera()
    {
        if (_bound != null && _bound.BoundingShape2D != null)
            _bound.BoundingShape2D = null;

        // 종료 후에는 첫 조우 컷씬의 블렌드 시간이 다른 전환에 적용되지 않도록 해제합니다.
        if (_isCameraBlendOverrideRegistered)
        {
            CinemachineCore.GetBlendOverride -= GetCameraBlend;
            _isCameraBlendOverrideRegistered = false;
        }

        RestoreCameraPriority();
    }

    private void RestoreCameraPriority()
    {
        if (!_hasCameraPriorityOverride)
            return;

        if (_cutsceneCamera != null)
            _cutsceneCamera.Priority = _cameraPriorityBeforeCutscene;

        _hasCameraPriorityOverride = false;
    }
    #endregion

}
