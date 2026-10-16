using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class StatuePuzzle : MonoBehaviour, ISaveable
{
    public const string RewardReceivedFlag = "StatuePuzzle_RewardReceived";
    private const string RewardEventFinishedFlag = "StatuePuzzle_RewardEventFinished";
    private const string CheckpointEntryID = "school_artroom_0";

    [System.Serializable]
    public struct StatueSetting
    {
        public StatuePuzzlePiece statue;
        public StatuePuzzlePiece.EDirection correctDirection;
    }

    [Header("Puzzle Settings")]
    [Tooltip("퍼즐에 포함된 조각상과 각각의 정답 방향 설정")]
    [SerializeField] private StatueSetting[] statues;
    [SerializeField] private DialogData solveDialog;

    [Header("Reward")]
    [Tooltip("퍼즐 해결 시 지급할 보상 아이템")]
    [SerializeField] private KeyItemData rewardItem;
    [SerializeField] private DialogData rewardDialog;
    
    [Tooltip("발자국 연출")]
    [SerializeField] private float fadeIntensity = 0.05f;
    [SerializeField] private FootprintSequence footprintSequence;
    [Tooltip("1층 교실의 더러운 책상 활성화")]
    [SerializeField] private Collider2D deskCollider;
    [Tooltip("힌트 초상화 되돌리기")]
    [SerializeField] private GameObject[] glowingEyes = new GameObject[4];

    [Header("정전 후 화장실 조명")]
    [SerializeField] private GameObject _doorLight;
    [SerializeField] private GameObject _toiletLight;

    [Tooltip("진엔딩")]
    [SerializeField] private Light2D spotLight;
    [SerializeField] private GameObject item;

    private bool _isSolved = false;
    private bool _hasReceivedReward;
    private bool _hasFinishedRewardEvent;
    private Coroutine _finishRoutine;
    private PlayerLight playerLight;

    private void Awake()
    {
        playerLight = FindFirstObjectByType<PlayerLight>();
    }

    private void Start()
    {
        foreach (var setting in statues)
        {
            if (setting.statue != null)
            {
                setting.statue.OnRotated += CheckPuzzleCompletion;
            }
        }

        if (deskCollider != null) deskCollider.enabled = false;

        if (spotLight != null) spotLight.intensity = 0f;
        if (item != null) item.SetActive(false);

        SaveManager.Instance?.Register(this);
    }

    private void CheckPuzzleCompletion()
    {
        if (_isSolved) return;

        foreach (var setting in statues)
        {
            if (setting.statue.CurrentDirection != setting.correctDirection)
            {
                return;
            }
        }

        SolvePuzzle();
    }

    private void SolvePuzzle()
    {
        _isSolved = true;

        ApplySolvedState();

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX("PuzzleComplete_Statue");

        if (rewardItem != null && InventoryManager.Instance != null)
        {
            Debug.Log("퍼즐 해결!! 키 아이템 획득");
            DialogSystem.Instance.PlayDialog(solveDialog, () => GetReward());
        }
    }

    private void ApplySolvedState()
    {
        foreach (var setting in statues)
        {
            if (setting.statue == null) continue;
            setting.statue.SetDirection(setting.correctDirection);
            setting.statue.enabled = false;

            Collider2D col = setting.statue.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }

        foreach (GameObject glowingEye in glowingEyes)
        {
            if (glowingEye != null) glowingEye.SetActive(false);
        }
    }

    private void GetReward()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AcquireItem(rewardItem, rewardDialog, OnRewardReceived);
        }
    }

    private void OnRewardReceived()
    {
        _hasReceivedReward = true;
        // 획득 대사가 끝난 뒤 저장하여 열쇠와 연출 시작 전 상태를 함께 남깁니다.
        SaveManager.Instance?.SaveMapCheckpoint(EMapType.SchoolArtroom, CheckpointEntryID);
        _finishRoutine = StartCoroutine(FinishPuzzleRoutine());
    }

    public string GetUniqueID() => RewardReceivedFlag;

    public void SaveState(SaveData data)
    {
        data.SetFlag(RewardReceivedFlag, _hasReceivedReward);
        data.SetFlag(RewardEventFinishedFlag, _hasFinishedRewardEvent);
    }

    public void LoadState(SaveData data)
    {
        if (_finishRoutine != null) StopCoroutine(_finishRoutine);
        _finishRoutine = null;
        ReleaseRewardEventControls();
        _hasReceivedReward = data.GetFlag(RewardReceivedFlag);
        _hasFinishedRewardEvent = data.GetFlag(RewardEventFinishedFlag);
        _isSolved = _hasReceivedReward;
        if (!_isSolved) return;

        ApplySolvedState();
        if (_hasFinishedRewardEvent)
        {
            // 이후 체크포인트에서는 연출 대신 그 시점의 오브젝트만 복원합니다.
            ApplyRewardEventState(data);
        }
        else
        {
            // 사망 직전 이미 주웠더라도 저장 당시 미획득이면 다시 주울 수 있게 준비합니다.
            if (item != null)
                item.GetComponent<ItemPickup>()?.RestorePickupState(data, showIfUncollected: false);
            _finishRoutine = StartCoroutine(ResumeRewardEventRoutine());
        }
    }

    private IEnumerator ResumeRewardEventRoutine()
    {
        // SaveManager의 배경/조명 복원과 화면 페이드가 끝난 뒤 깜빡임을 재생합니다.
        yield return new WaitUntil(() => SaveManager.Instance == null || !SaveManager.Instance.IsLoading);
        yield return FinishPuzzleRoutine();
    }

    private void OnDestroy()
    {
        SaveManager.Instance?.Unregister(this);
        foreach (var setting in statues)
        {
            if (setting.statue != null)
            {
                setting.statue.OnRotated -= CheckPuzzleCompletion;
            }
        }
    }

    private IEnumerator FinishPuzzleRoutine()
    {
        ControlLockManager.Instance?.AddMovementLock(this);
        ControlLockManager.Instance?.AddInputLock(this);
        MapManager.Instance?.Player?.GetComponent<PlayerMovement>()?.StopImmediately();

        try
        {
            yield return new WaitForSeconds(1f);

            SoundManager.Instance?.PlaySFX("Fluorescent_Flicker");
            yield return LightingEventManager.Instance?.FlickerRoutine();
            yield return new WaitForSeconds(1f);
            SoundManager.Instance?.PlaySFX("Fluorescent_Blackout");
            LightingEventManager.Instance?.SetBlackout(fadeIntensity, 0.2f);
            yield return new WaitForSeconds(1f);

            // 발자국 재생을 시작한 프레임에 잠금을 풀어, 나머지 발자국은 이동하며 볼 수 있게 합니다.
            ApplyRewardEventState();
            _hasFinishedRewardEvent = true;
        }
        finally
        {
            ReleaseRewardEventControls();
            _finishRoutine = null;
        }
    }

    private void ReleaseRewardEventControls()
    {
        ControlLockManager.Instance?.RemoveMovementLock(this);
        ControlLockManager.Instance?.RemoveInputLock(this);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _finishRoutine = null;
        ReleaseRewardEventControls();
    }

    private void ApplyRewardEventState(SaveData data = null)
    {
        if (_doorLight != null) _doorLight.SetActive(true);
        if (_toiletLight != null) _toiletLight.SetActive(true);

        bool deskCompleted = data != null && data.GetFlag(DirtyDeskCutsceneController.CompletedFlag);
        if (deskCompleted) footprintSequence?.HideFootprint();
        else if (data == null) footprintSequence?.PlayFootprintSequence();
        else footprintSequence?.ShowFootprints();

        if (playerLight != null)
        {
            if (deskCompleted) playerLight.TurnOff(false);
            else playerLight.TurnOn();
        }
        
        if (deskCollider != null) deskCollider.enabled = !deskCompleted;

        if (spotLight != null) spotLight.intensity = 5f;
        if (item != null)
        {
            ItemPickup pickup = item.GetComponent<ItemPickup>();
            if (pickup != null && data != null) pickup.RestorePickupState(data);
            else item.SetActive(true);
        }
    }
}
