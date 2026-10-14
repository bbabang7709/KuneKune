using System.Collections;
using UnityEngine;

public class Locker : MonoBehaviour, IInteractable
{
    [SerializeField] private bool isMyLocker = false;
    [SerializeField] private DialogData beforeDialog;
    [SerializeField] private DialogData afterDialog;
    [SerializeField] private DialogData notMyLockerDialog;
    [SerializeField] private DialogData allDollPartsDialog;

    [Header("Item")]
    [SerializeField] private ItemData sewingTools;
    [SerializeField] private DialogData acquireDialog;

    [Header("Puzzle Settings")]
    [Tooltip("인형 봉합 퍼즐 UI (DollPuzzleManager가 붙어있는 Doll Puzzle Canvas)")]
    [SerializeField] private GameObject puzzleUIRoot;
    [Tooltip("퍼즐 완성 후 완성된 인형을 보여주는 시간(초)")]
    [SerializeField] private float closeDelay = 1.5f;

    private DollPuzzleManager puzzleManager;
    private bool hasAcquired = false;
    private bool hasSolved = false;
    private bool canOpen = false;
    private bool isPuzzleOpen = false;
    private bool hasShownSewingHint;

    private bool HasAllDollParts => InventoryManager.Instance != null &&
        InventoryManager.Instance.HasKey("doll_head") &&
        InventoryManager.Instance.HasKey("doll_arm") &&
        InventoryManager.Instance.HasKey("doll_leg");

    private void Awake()
    {
        if (allDollPartsDialog == null)
            allDollPartsDialog = Resources.Load<DialogData>("Dialogs/School/Doll_Parts_Complete");
        if (puzzleUIRoot == null) return;

        puzzleManager = puzzleUIRoot.GetComponentInChildren<DollPuzzleManager>(true);
        puzzleUIRoot.SetActive(false);
    }

    private void Update()
    {
        if (!isMyLocker || hasSolved) return;
        canOpen = HasAllDollParts;
        if (!canOpen || hasShownSewingHint || allDollPartsDialog == null || DialogSystem.Instance == null ||
            DialogSystem.Instance.IsDialogPlaying || Time.timeScale <= 0f ||
            (SaveManager.Instance != null && SaveManager.Instance.IsLoading) ||
            (CutsceneManager.Instance != null && CutsceneManager.Instance.IsCutscenePlaying) ||
            (ControlLockManager.Instance != null && ControlLockManager.Instance.IsInputLocked)) return;

        hasShownSewingHint = true;
        DialogSystem.Instance.PlayDialog(allDollPartsDialog);
    }

    private void OnEnable()
    {
        if (puzzleManager != null) puzzleManager.onPuzzleComplete.AddListener(OnPuzzleSolved);
    }

    private void OnDisable()
    {
        if (puzzleManager != null) puzzleManager.onPuzzleComplete.RemoveListener(OnPuzzleSolved);

        if (isPuzzleOpen) ReleaseLock();
    }

    public void OnInteractPressed()
    {
        if (isMyLocker && canOpen)
        {
            if (hasSolved)
            {
                DialogSystem.Instance?.PlayDialog(afterDialog);
            }
            else if (!hasAcquired)
            {
                // 대화가 끝난 뒤 바느질 도구 획득 -> 획득 연출이 끝나면 인형 봉합 UI 오픈
                if (beforeDialog != null) DialogSystem.Instance?.PlayDialog(beforeDialog, AcquireSewingTools);
                else AcquireSewingTools();
            }
            else
            {
                StartPuzzle();
            }
        }
        else
        {
            DialogSystem.Instance?.PlayDialog(notMyLockerDialog);
        }
    }

    private void AcquireSewingTools()
    {
        hasAcquired = true;
        InventoryManager.Instance?.AcquireItem(sewingTools, acquireDialog, StartPuzzle);
    }

    private void StartPuzzle()
    {
        if (puzzleUIRoot == null || isPuzzleOpen || hasSolved) return;

        isPuzzleOpen = true;
        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.AddInputLock(this);
            ControlLockManager.Instance.AddMovementLock(this);
        }

        puzzleUIRoot.SetActive(true);
    }

    private void ReleaseLock()
    {
        isPuzzleOpen = false;
        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.RemoveInputLock(this);
            ControlLockManager.Instance.RemoveMovementLock(this);
        }
    }

    public void EnableOpen()
    {
        canOpen = HasAllDollParts;
    }

    /// <summary>
    /// 인형 봉합 완료 시 DollPuzzleManager.onPuzzleComplete로 호출된다.
    /// 완성된 인형을 잠시 보여준 뒤 UI를 닫고 조작 잠금을 해제한다.
    /// </summary>
    public void OnPuzzleSolved()
    {
        if (hasSolved) return;

        hasSolved = true;
        StartCoroutine(ClosePuzzleRoutine());
    }

    private IEnumerator ClosePuzzleRoutine()
    {
        yield return new WaitForSeconds(closeDelay);

        if (puzzleUIRoot != null) puzzleUIRoot.SetActive(false);
        ReleaseLock();
    }
}
