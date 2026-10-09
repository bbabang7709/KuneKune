using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 집 안 추격의 시작과 종료 상태를 관리합니다.
/// 추격 중에는 지정된 현관문과 화장실 문을 열 수 없게 합니다.
/// </summary>
[DisallowMultipleComponent]
public class HouseChase : MonoBehaviour
{
    private Door[] _houseDoors;
    private PlayerAppearance _player;

    [SerializeField] private Transform _playerFallPosition;
    [SerializeField] private Transform _kunekuneFallPosition;

    public static HouseChase Instance { get; private set; }
    public bool IsChaseActive { get; private set; }

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        Instance = this;
        _houseDoors = GetComponentsInChildren<Door>(true);
        _player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerAppearance>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// 추격을 시작하고, 추격 중 잠가야 하는 문을 모두 잠급니다.
    /// </summary>
    public void BeginChase()
    {
        if (IsChaseActive)
            return;

        IsChaseActive = true;
        SetChaseDoorsLocked(true);

        // 맵 전환이 끝난 뒤 배치해야 쿠네쿠네도 도착한 맵에서 추적을 시작합니다.
        MapManager.Instance.ChangeMap(EMapType.House_Hallway, "houseChase_playerFallPosition", onComplete: () =>
        {
            // 전환을 기다리는 동안 추격이 끝났다면 다시 소환하지 않습니다.
            if (this == null || IsChaseActive == false)
                return;

            Physics2D.SyncTransforms();
            MapManager.Instance.KuneKune.Spawn(_kunekuneFallPosition.position, true);

            Debug.Log("[HouseChaseController] 집 안 추격을 시작했습니다.", this);
        });
    }

    /// <summary>
    /// 플레이어가 잡히면 쿠네쿠네를 사라지게 하고 추격을 끝냅니다.
    /// </summary>
    public void EndChase()
    {
        if (Instance != this)
            return;

        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.AddInputLock(this);
            ControlLockManager.Instance.AddMovementLock(this);
        }

        IsChaseActive = false;
        SetChaseDoorsLocked(false);
        MapManager.Instance.KuneKune.Despawn();

        // TODO: 플레이어 학교 1층으로 이동
        // MapManager.Instance.ChangeMap(EMapType.School_Hallway, "school_hallway_0", () => _player.SetAge(EPlayerAge.Student));
        StartCoroutine(ChangeChapterRoutine());

        Debug.Log("[HouseChaseController] 집 안 추격을 끝냈습니다.", this);
    }

    private IEnumerator ChangeChapterRoutine()
    {
        yield return UIManager.Instance?.FadeOut(1.5f);

        MapManager.Instance?.InitMap(EMapType.School_Hallway, "school_hallway_0");
        _player.SetAge(EPlayerAge.Student);

        yield return UIManager.Instance?.FadeIn(1.5f);
        
        SaveManager.Instance?.SaveChapterCheckpoint(EChapter.Chapter3_School);
        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.RemoveInputLock(this);
            ControlLockManager.Instance.RemoveMovementLock(this);
        }
    }

    private void SetChaseDoorsLocked(bool isLocked)
    {
        foreach (Door door in _houseDoors)
        {
            if (door == null || door.LockDuringChase == false)
                continue;
            door.SetHouseChaseLocked(isLocked);
        }
    }
}
