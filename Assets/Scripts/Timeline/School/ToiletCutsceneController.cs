using System.Collections;
using UnityEngine;

[RequireComponent(typeof(DialogTimelineController))]
[RequireComponent(typeof(BoxCollider2D))]
public class ToiletCutsceneController : CutsceneControllerBase
{
    private const string FirstEscapeFlag = "HasSeenFirstToiletEscape";

    [Header("화장실 진입 대사")]
    [SerializeField] private DialogData _normalDialog;
    [SerializeField] private DialogData _firstEscapeDialog;
    [SerializeField] private DialogData _repeatEscapeDialog;
    
    private ToiletTrueFlagCutsceneController _toiletTrueFlagCutscene;

    private Collider2D _collider;
    private DialogTimelineController _dialogController;
    private bool _isEntryPlaying;
    private bool _firstEscapeShownWithoutSaveManager;

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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player") || _isEntryPlaying || (playOnlyOnce && hasPlayed))
            return;

        // 진엔딩 플래그를 만족하면 화장실 진입 대사를 재생하지 않는다.
        // 진엔딩 플래그 컷씬이 재생된적이 있다면, 화장실 진입 대사가 다시 나온다.
        if (_toiletTrueFlagCutscene != null && _toiletTrueFlagCutscene.HasPlayed == false
            && InventoryManager.Instance != null && InventoryManager.Instance.HasKey("hanky"))
        {
            _toiletTrueFlagCutscene.Play();
            return;
        }

        // test
        //if (_toiletTrueFlagCutscene != null && _toiletTrueFlagCutscene.HasPlayed == false)
        //{
        //    _toiletTrueFlagCutscene.Play();
        //    return;
        //}

        _isEntryPlaying = true;
        // 대기 코루틴이 PlayCutscene을 호출할 때까지의 프레임 틈에 조작이 새지 않게 먼저 잠급니다.
        HoldControls();
        bool wasBeingChased = MapManager.Instance != null && MapManager.Instance.ConsumeChasedMapEntry();
        StartCoroutine(WaitAndPlayCutscene(wasBeingChased));
    }

    private IEnumerator WaitAndPlayCutscene(bool wasBeingChased)
    {
        if (SaveManager.Instance != null)
        {
            yield return new WaitUntil(() => SaveManager.Instance.IsLoading == false);
            // 로드가 끝나며 잠금이 모두 지워질 수 있으므로 다시 잠급니다.
            HoldControls();
        }

        yield return new WaitUntil(() =>
            (CutsceneManager.Instance == null || !CutsceneManager.Instance.IsCutscenePlaying) &&
            (DialogSystem.Instance == null || !DialogSystem.Instance.IsDialogPlaying));

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
        if (!_isEntryPlaying || dialog != _firstEscapeDialog)
            return;

        // 첫 대사가 시작되면 기록하며, 다음 체크포인트 저장 때 저장 파일에 반영합니다.
        _firstEscapeShownWithoutSaveManager = true;
        SaveManager.Instance?.SetFlag(FirstEscapeFlag, true);
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
