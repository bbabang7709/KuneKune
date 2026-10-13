using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어가 상호작용하여 다른 위치로 이동할 수 있는 문입니다.
/// </summary>
public class Door : MapTransition, IInteractable
{
    [Header("Lock Settings")]
    [SerializeField] private bool isLocked;
    [SerializeField] private string requiredKeyID;
    [SerializeField] private DialogData dialogWhenLocked;
    [SerializeField] private DialogData dialogWhenUnlock;

    [Header("추격 잠금 설정")]
    [Tooltip("집 안 추격이 시작되면 이 문을 열 수 없게 합니다.")]
    [SerializeField] private bool _lockDuringChase;
    [SerializeField] private DialogData dialogWhenChase;

    [Header("Door Visuals")]
    [Tooltip("문이 열렸을 때 보여줄 스프라이트 등록")]
    [SerializeField] private Sprite openSprite;
    private Sprite _closedSprite;
    private SpriteRenderer sr;

    [Header("Sound Settings")]
    [Tooltip("문이 열릴 때 재생할 SoundID")]
    [SerializeField] private string openSoundID = "DoorOpen";

    [Header("Transition Timing")]
    [Tooltip("문이 열린 후 맵 이동이 시작되기까지의 딜레이")]
    [SerializeField] private float transitionDelay = 0.2f;

    [Tooltip("화면이 완전히 암전되어 맵 이동이 끝나는 데 걸리는 시간. 이 시간이 지나면 문이 닫힙니다.")]
    [SerializeField] private float resetDelay = 1.5f;

    private bool _isTransitioning = false;

    private bool _isLockedByChase;

    public bool LockDuringChase => _lockDuringChase;

    void Awake()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null) _closedSprite = sr.sprite;
    }

    public void OnInteractPressed()
    {
        if (_isTransitioning) return; 

        // 추격 잠금은 열쇠 보유 여부와 상관없이 문을 열 수 없습니다.
        if (_isLockedByChase)
        {
            if (DialogSystem.Instance != null) DialogSystem.Instance.PlayDialog(dialogWhenChase);

            return;
        }

        // 부모 클래스(MapTransition)의 프로퍼티를 확인
        if (TargetEntryPoint == null || TargetMap == null)
        {
            Debug.LogWarning($"[{nameof(Door)}] Target Map 또는 Entry Point가 설정되지 않았습니다.", this);
            return;
        }

        if (isLocked && !CheckCanOpen())
        {
            PlayLockedDialog();
            return;
        }

        // 필요한 열쇠가 있다면 잠금 상태를 해제합니다.
        if (isLocked)
        {
            isLocked = false;
            DialogSystem.Instance?.PlayDialog(
                dialogWhenUnlock,
                () => StartCoroutine(OpenDoorRoutine())
            );
            return;
        }

        // 부모에 구현된 ID 기반 이동 메서드 호출 (페이드 연출 자동 수행)
        StartCoroutine(OpenDoorRoutine());
    }

    private IEnumerator OpenDoorRoutine()
    {
        _isTransitioning = true;

        if (SoundManager.Instance != null && !string.IsNullOrEmpty(openSoundID))
        {
            SoundManager.Instance.PlaySFX(openSoundID);
        }

        if (sr != null && openSprite != null)
        {
            sr.sprite = openSprite;
        }

        yield return new WaitForSeconds(transitionDelay);

        ExecuteTransition();

        yield return new WaitForSeconds(resetDelay);

        if (sr != null)
        {
            sr.sprite = _closedSprite;
        }

        _isTransitioning = false;
    }

    public void SetHouseChaseLocked(bool isLocked)
    {
        _isLockedByChase = isLocked;
    }

    private bool CheckCanOpen()
    {
        if (string.IsNullOrEmpty(requiredKeyID))
        {
            return true;
        }

        return InventoryManager.Instance != null &&
               InventoryManager.Instance.HasKey(requiredKeyID);
    }

    private void PlayLockedDialog()
    {
        if (dialogWhenLocked == null) return;
        DialogSystem.Instance?.PlayDialog(dialogWhenLocked);
    }

    public void SetLockState(bool state)
    {
        isLocked = state;
    }
}