using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현재 Map 정보와 Map 이동을 관리하는 Singlton Class.
/// </summary>
public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    public MapArea CurrentMap { get; private set; } // 현재 Map 정보
    public HashSet<Transform> KunekuneSpawnPoints { get; private set; } = new();

    private GameObject player;

    public KuneKune KuneKune { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        player = GameObject.FindGameObjectWithTag("Player");
        KuneKune = KuneKune.Instantiate();
    }

    /// <summary>
    /// CurrentMap을 갱신하는 메서드.
    /// </summary>
    /// <param name="map">Target map to change CurrentMap</param>
    public void SetCurrentMap(MapArea map)
    {
        if (map == null) return;
        CurrentMap = map;
        KunekuneSpawnPoints.Clear();

        Debug.Log(
                $"현재 Map: {CurrentMap.MapType} " +
                $"Map ID: {CurrentMap.MapID}"
            );

        if (map.KuneKune_ables == null)
            return;

        Transform parent = map.KuneKune_ables.transform;
        Transform[] spawnPoints = parent.GetComponentsInChildren<Transform>();
        foreach (Transform sp in spawnPoints)
        {
            if (sp == parent) // 부모 제외
                continue;
            KunekuneSpawnPoints.Add(sp);
        }
    }

    /// <summary>
    /// 플레이어의 위치를 targetMap의 targetEntryPoint 위치로 이동시키는 메서드.
    /// </summary>
    /// <param name="targetMap">이동할 Map</param>
    /// <param name="targetEntryPoint">이동할 위치</param>
    public void ChangeMap(MapArea targetMap, EntryPoint targetEntryPoint)
    {
        if (targetMap == null || targetEntryPoint == null) return;
        
        SetCurrentMap(targetMap);
        player.transform.position = targetEntryPoint.SpawnPoint.position;
        CameraManager.Instance.SetCamera(targetEntryPoint.MapCamera);
    }
}
