using System.Collections;
using UnityEngine;

[RequireComponent(typeof(DialogTimelineController))]
[RequireComponent(typeof(BoxCollider2D))]
public class ToiletCutsceneController : CutsceneControllerBase
{
    private const string FirstEscapeFlag = "HasSeenFirstToiletEscape";
    private const string FirstTrueFlagFailDialogFlag = "HasSeenFirstToiletTrueFlagFail";

    [Header("화장실 진입 대사")]
    [SerializeField] private DialogData _normalDialog;
    [SerializeField] private DialogData _firstEscapeDialog;
    [SerializeField] private DialogData _repeatEscapeDialog;
    [SerializeField] private DialogData _firstTrueFlagFailDialog;
    [SerializeField] private DialogData _repeatTrueFlagFailDialog;

    private ToiletTrueFlagCutsceneController _toiletTrueFlagCutscene;

    private Collider2D _collider;
    private DialogTimelineController _dialogController;
    private bool _isEntryPlaying;
    private bool _firstEscapeShownWithoutSaveManager;
    private bool _firstTrueFlagFailShownWithoutSaveManager;

    protected override void Awake()
    {
        base.Awake();

        _toiletTrueFlagCutscene = FindFirstObjectByType<ToiletTrueFlagCutsceneController>(FindObjectsInactive.Include);

        _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true;
        _collider.excludeLayers = ~LayerMask.GetMask("Player");

        _dialogController = GetComponent<DialogTimelineController>();
        _dialogController.DialogStarted += OnDialogStarted;
    }

    private bool CanPlayTrueFlagCutscene()
    {
        if (_toiletTrueFlagCutscene != null && _toiletTrueFlagCutscene.HasTrueFlagSucceeded)
            return false;

        // test
        //if (_toiletTrueFlagCutscene != null && _toiletTrueFlagCutscene.HasPlayed == false &&
        //    !_toiletTrueFlagCutscene.HasTrueFlagFailed)
        //{
        //    return true;
        //}

        if (_toiletTrueFlagCutscene == null || _toiletTrueFlagCutscene.HasPlayed ||
            _toiletTrueFlagCutscene.HasTrueFlagFailed || InventoryManager.Instance == null ||
            !InventoryManager.Instance.HasKey("hanky")) 
            return false;

        return true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_isEntryPlaying || (playOnlyOnce && hasPlayed))
            return;

        _isEntryPlaying = true;
        // 대기 코루틴이 PlayCutscene을 호출할 때까지의 프레임 틈에 조작이 새지 않게 먼저 잠급니다.
        HoldControls();
        StartCoroutine(WaitAndPlayCutscene());
    }

    private IEnumerator WaitAndPlayCutscene()
    {
        // 컷씬과 대사는 저장 파일을 읽고 맵 복원이 끝난 뒤의 상태로 선택한다.
        yield return new WaitUntil(() =>
            (SaveManager.Instance == null || !SaveManager.Instance.IsLoading) &&
            (CutsceneManager.Instance == null || !CutsceneManager.Instance.IsCutscenePlaying) &&
            (DialogSystem.Instance == null || !DialogSystem.Instance.IsDialogPlaying));

        // 로드가 끝나며 잠금이 모두 지워질 수 있으므로 다시 잠급니다.
        HoldControls();

        // 진엔딩 플래그를 만족하면 화장실 진입 대사를 재생하지 않는다.
        // 진엔딩 플래그 컷씬이 재생된적이 있다면, 화장실 진입 대사가 다시 나온다.
        if (CanPlayTrueFlagCutscene())
        {
            _toiletTrueFlagCutscene.Play();
            _isEntryPlaying = false;

            ReleaseControls();
            yield break;
        }

        bool wasBeingChased = MapManager.Instance != null && MapManager.Instance.ConsumeChasedMapEntry();
        DialogData selectedDialog = SelectEntryDialog(wasBeingChased);
        if (_normalDialog == null || selectedDialog == null)
        {
            Debug.LogError("화장실 진입 대사를 지정해주세요.", this);
            _isEntryPlaying = false;
            ReleaseControls();
            yield break;
        }

        _dialogController.SetDialogOverride(_normalDialog, selectedDialog);
        PlayCutscene();

        // 컷씬이 시작되지 않았다면 다음 진입을 막지 않도록 되돌립니다.
        if (!IsCutsceneRunning)
        {
            _dialogController.ClearDialogOverride();
            _isEntryPlaying = false;
        }
    }

    private DialogData SelectEntryDialog(bool wasBeingChased)
    {
        // 진엔딩 플래그에 실패한 이후에는 일반/피신 대사보다 실패 대사를 우선한다.
        if (_toiletTrueFlagCutscene != null && _toiletTrueFlagCutscene.HasTrueFlagFailed)
        {
            bool hasSeenFirstFailure = SaveManager.Instance != null
                    ? SaveManager.Instance.GetFlag(FirstTrueFlagFailDialogFlag)
                    : _firstTrueFlagFailShownWithoutSaveManager;

            return hasSeenFirstFailure ? _repeatTrueFlagFailDialog : _firstTrueFlagFailDialog;
        }

        // 쿠네쿠네에게 쫓기는 상황이 아닌 대사
        if (!wasBeingChased)
            return _normalDialog;

        // 쿠네쿠네에게 쫓기는 상황일 때, 화장실에 처음 들어오는 경우인지 확인
        bool hasSeenFirstEscape = SaveManager.Instance != null
            ? SaveManager.Instance.GetFlag(FirstEscapeFlag)
            : _firstEscapeShownWithoutSaveManager;

        // 첫 피신 대사를 이미 봤는지 확인
        return hasSeenFirstEscape ? _repeatEscapeDialog : _firstEscapeDialog;
    }

    private void OnDialogStarted(DialogData dialog)
    {
        if (!_isEntryPlaying)
            return;

        // 첫 대사가 시작되면 기록하며, 다음 체크포인트 저장 때 저장 파일에 반영합니다.
        if (dialog == _firstTrueFlagFailDialog)
        {
            _firstTrueFlagFailShownWithoutSaveManager = true;
            SaveManager.Instance?.SetFlag(FirstTrueFlagFailDialogFlag, true);
        }
        else if (dialog == _firstEscapeDialog)
        {
            _firstEscapeShownWithoutSaveManager = true;
            SaveManager.Instance?.SetFlag(FirstEscapeFlag, true);
        }
    }

    protected override void OnCutsceneFinished()
    {
        _dialogController.ClearDialogOverride();
        _isEntryPlaying = false;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        StopAllCoroutines();
        _dialogController.ClearDialogOverride();
        _isEntryPlaying = false;
    }

    private void OnDestroy()
    {
        if (_dialogController != null)
            _dialogController.DialogStarted -= OnDialogStarted;
    }
}
