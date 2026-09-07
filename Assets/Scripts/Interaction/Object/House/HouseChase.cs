using System;
using UnityEngine;

/// <summary>
/// 집 안 추격의 시작과 종료 상태를 관리합니다.
/// 추격 중에는 지정된 현관문과 화장실 문을 열 수 없게 합니다.
/// </summary>
[DisallowMultipleComponent]
public class HouseChase : MonoBehaviour
{
    private Door[] _houseDoors;
    private PlayerController _player;

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
        _player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
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

        _player.transform.position = _playerFallPosition.position;
        MapManager.Instance.KuneKune.Spawn(_kunekuneFallPosition.position);

        Debug.Log("[HouseChaseController] 집 안 추격을 시작했습니다.", this);
    }

    /// <summary>
    /// 플레이어가 잡히면 쿠네쿠네를 사라지게 하고 추격을 끝냅니다.
    /// </summary>
    public void EndChase()
    {
        if (Instance != this)
            return;

        IsChaseActive = false;
        SetChaseDoorsLocked(false);
        MapManager.Instance.KuneKune.Despawn();

        // TODO: 플레이어 학교 1층으로 이동

        Debug.Log("[HouseChaseController] 집 안 추격을 끝냈습니다.", this);
    }

    private void SetChaseDoorsLocked(bool isLocked)
    {
        foreach (Door door in _houseDoors)
        {
            if (door == null || door.LockDuringHouseChase == false)
                continue;
            door.SetHouseChaseLocked(isLocked);
        }
    }
}
