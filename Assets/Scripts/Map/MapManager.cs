using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현재 Map 정보와 Map 이동, 쿠네쿠네 경로 탐색을 관리하는 Singleton Class.
/// </summary>
public class MapManager : MonoBehaviour
{
    private class RouteSearchNode
    {
        public IKuneKuneMapRoute Route { get; }
        /// <summary>
        /// 쿠네쿠네의 현재 위치에서 여러 문을 거쳐 플레이어가 있는 맵에 들어가기까지의 거리
        /// </summary>
        public float Distance { get; }

        public RouteSearchNode(IKuneKuneMapRoute route, float distance)
        {
            Route = route;
            Distance = distance;
        }
    }
    
    public static MapManager Instance { get; private set; }

    [Header("Map Areas")]
    [Tooltip("씬에 존재하는 모든 MapArea 객체를 넣어주세요.")]
    [SerializeField] private List<MapArea> allMapAreas = new List<MapArea>();
    [Tooltip("CoolZone으로 설정할 MapArea 객체를 넣어주세요.")]
    [SerializeField] private List<MapArea> allCoolZones = new List<MapArea>();

    public EMapType CurrentMapType { get; private set; } = EMapType.Outside_Busstop;
    private bool _wasChasedOnMapEntry;

    public bool ConsumeChasedMapEntry()
    {
        bool wasChased = _wasChasedOnMapEntry;
        _wasChasedOnMapEntry = false;
        return wasChased;
    }
    
    private MapArea _currentArea;
    public MapArea CurrentMap => _currentArea; // 쿠네쿠네 및 외부 참조용

    public HashSet<Transform> KunekuneSpawnPoints { get; private set; } = new();

    public GameObject Player { get; private set; }

    public KuneKune KuneKune { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Player = GameObject.FindGameObjectWithTag("Player");
        KuneKune = KuneKune.Instantiate();
        QuestManager.Instantiate();
    }

    /// <summary>
    /// 맵이 갱신될 때 쿠네쿠네의 스폰 포인트 정보를 업데이트합니다.
    /// (기존 SetCurrentMap 내부 로직 추출)
    /// </summary>
    private void UpdateKunekuneSpawns(MapArea map)
    {
        if (map == null) return;
        
        KunekuneSpawnPoints.Clear();

        Debug.Log($"현재 Map: {map.mapType} | Map ID: {map.gameObject.name}");

        // 팀원분 코드: KuneKune_ables가 없으면 리턴
        if (map.KuneKune_ables == null)
            return;

        // 비활성화된 사물 활성화
        if (KuneKune.SpawnObject != null)
            KuneKune.SpawnObject.gameObject.SetActive(true);

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
    /// 게임 시작 및 세이브 로드 시 화면 연출(페이드) 없이 즉시 맵을 세팅합니다.
    /// </summary>
    public void InitMap(EMapType targetMapType, string entryID)
    {
        // 저장 로드는 피신으로 취급하지 않고, 거울 등 인게임 즉시 이동은 추격 상황을 보존합니다.
        _wasChasedOnMapEntry = (SaveManager.Instance == null || !SaveManager.Instance.IsLoading) &&
            KuneKune != null && KuneKune.IsChaseActive;

        _currentArea = allMapAreas.Find(m => m.mapType == targetMapType);

        if (_currentArea != null)
        {
            CurrentMapType = targetMapType;
            
            RefreshMapBGM();

            UpdateKunekuneSpawns(_currentArea);

            Transform spawnPoint = _currentArea.GetEntryPoint(entryID);
            if (spawnPoint != null && Player != null)
            {
                Player.transform.position = spawnPoint.position;

                Physics2D.SyncTransforms();

                PlayerAppearance playerApp = Player.GetComponent<PlayerAppearance>();
                if (playerApp != null)
                {
                    playerApp.RefreshCollider();
                }
            }

            CameraManager.Instance?.ChangeCameraWithFollow(_currentArea.AreaCamera);
        }
        else
        {
            Debug.LogError($"[MapManager] {targetMapType} 맵을 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 인게임 텔레포트 시 사용합니다. 페이드 인/아웃 및 조작 잠금을 포함합니다.
    /// </summary>
    public void ChangeMap(EMapType targetMapType, string entryID, Action onComplete = null)
    {
        StartCoroutine(ChangeMapRoutine(targetMapType, entryID, onComplete));
    }

    private IEnumerator ChangeMapRoutine(EMapType targetMapType, string entryID, Action onComplete)
    {
        // 도착 후 더위 감소로 Despawn되어도, 이동 직전의 추격 상황을 보존합니다.
        bool wasChased = KuneKune != null && KuneKune.IsChaseActive;
        Time.timeScale = 0f;

        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.AddMovementLock(this);
            ControlLockManager.Instance.AddInputLock(this);
        }

        yield return UIManager.Instance?.FadeOut(0.5f);

        _currentArea = allMapAreas.Find(m => m.mapType == targetMapType);

        if (_currentArea != null)
        {
            CurrentMapType = targetMapType;
            _wasChasedOnMapEntry = wasChased;
            
            RefreshMapBGM();

            UpdateKunekuneSpawns(_currentArea); // 이동한 맵의 쿠네쿠네 스폰 포인트 갱신

            Transform spawnPoint = _currentArea.GetEntryPoint(entryID);
            if (spawnPoint != null && Player != null)
            {
                Player.transform.position = spawnPoint.position;

                Physics2D.SyncTransforms();

                PlayerAppearance playerApp = Player.GetComponent<PlayerAppearance>();
                if (playerApp != null)
                {
                    playerApp.RefreshCollider();
                }
            }

            CameraManager.Instance?.ChangeCameraWithFollow(_currentArea.AreaCamera);
        }
        else
        {
            Debug.LogError($"[MapManager] {targetMapType} 맵을 찾을 수 없습니다.");
        }

        yield return UIManager.Instance?.FadeIn(0.5f);
        
        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.RemoveMovementLock(this);
            ControlLockManager.Instance.RemoveInputLock(this);
        }

        Time.timeScale = 1f;

        onComplete?.Invoke();
    }

    /// <summary>
    /// 화장실에서는 음악을 멈추고, 추격 중이 아니면 현재 맵의 기본 음악을 재생합니다.
    /// </summary>
    public void RefreshMapBGM()
    {
        if (_currentArea == null)
            return;

        if (IsCoolZone(CurrentMapType) || CurrentMapType == EMapType.School_WomanToilet)
        {
            SoundManager.Instance?.StopBGM();
            return;
        }

        if (KuneKune != null && KuneKune.gameObject.activeInHierarchy)
            return;

        SoundManager.Instance?.PlayBGM(_currentArea.BgmID,
            volumeMultiplier: _currentArea.IsInside ? 0.35f : 1f);
    }

    public bool IsCoolZone(EMapType current)
    {
        return allCoolZones.Find(m => m.mapType == current) != null ? true : false;
    }

    // =========================================================
    // 쿠네쿠네 경로 탐색 로직 (팀원 원본 알고리즘 보존)
    // =========================================================
    public Queue<IKuneKuneMapRoute> FindRoute(MapArea startMap, Vector3 startPosition, MapArea targetMap, Vector3 targetPosition)
    {
        Queue<IKuneKuneMapRoute> emptyRoute = new();
        if (startMap == null || targetMap == null || startMap == targetMap)
            return emptyRoute;

        List<RouteSearchNode> remainingNodes = new();
        Dictionary<IKuneKuneMapRoute, float> minDistances = new();
        Dictionary<IKuneKuneMapRoute, IKuneKuneMapRoute> previousRoutes = new();

        foreach (IKuneKuneMapRoute route in GetRoutesFromMap(startMap))
        {
            float distance = Mathf.Abs(startPosition.x - route.TransitionPoint.position.x);
            minDistances[route] = distance;
            previousRoutes[route] = null;
            remainingNodes.Add(new RouteSearchNode(route, distance));
        }

        IKuneKuneMapRoute lastRoute = null;
        float minTotalDistance = Mathf.Infinity;

        while (remainingNodes.Count > 0)
        {
            int nearestNodeIdx = FindNearestNodeIndex(remainingNodes);
            RouteSearchNode currentNode = remainingNodes[nearestNodeIdx];
            remainingNodes.RemoveAt(nearestNodeIdx);

            if (currentNode.Distance >= minTotalDistance)
                break;

            if (minDistances.TryGetValue(currentNode.Route, out float distance) && currentNode.Distance > distance)
                continue;

            EntryPoint arrivalEntryPoint = currentNode.Route.TargetEntryPoint;
            
            if (arrivalEntryPoint == null)
                continue;

            Vector3 spawnPosition = arrivalEntryPoint.transform.position;

            // 플레이어가 있는 맵 루트를 찾았다
            if (currentNode.Route.TargetMap == targetMap)
            {
                float totalDistance = currentNode.Distance + Mathf.Abs(spawnPosition.x - targetPosition.x);
                if (totalDistance < minTotalDistance)
                {
                    minTotalDistance = totalDistance;
                    lastRoute = currentNode.Route;
                }

                continue;
            }

            // 현재 루트에 있는 다음 문 찾기
            foreach (IKuneKuneMapRoute nextRoute in GetRoutesFromMap(currentNode.Route.TargetMap))
            {
                float nextDistance = currentNode.Distance + Mathf.Abs(spawnPosition.x - nextRoute.TransitionPoint.position.x);
                if (minDistances.TryGetValue(nextRoute, out float previousDistance) && previousDistance <= nextDistance)
                    continue;

                minDistances[nextRoute] = nextDistance;
                previousRoutes[nextRoute] = currentNode.Route;
                remainingNodes.Add(new RouteSearchNode(nextRoute, nextDistance));
            }
        }

        return ReverseRouteQueue(lastRoute, previousRoutes);
    }

    private Queue<IKuneKuneMapRoute> ReverseRouteQueue(IKuneKuneMapRoute lastRoute, Dictionary<IKuneKuneMapRoute, IKuneKuneMapRoute> previousRoutes)
    {
        List<IKuneKuneMapRoute> routes = new();
        IKuneKuneMapRoute route = lastRoute;

        while (route != null)
        {
            routes.Add(route);
            if (previousRoutes.TryGetValue(route, out IKuneKuneMapRoute previousRoute) == false)
                break;

            route = previousRoute;
        }

        routes.Reverse();
        return new Queue<IKuneKuneMapRoute>(routes);
    }

    private List<IKuneKuneMapRoute> GetRoutesFromMap(MapArea sourceMap)
    {
        List<IKuneKuneMapRoute> routes = new();
        if (sourceMap == null)
            return routes;

        MonoBehaviour[] components = sourceMap.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour component in components)
        {
            if (component is not IKuneKuneMapRoute route)
                continue;

            if (route.SourceMap != sourceMap || route.TargetMap == null ||
                route.TargetEntryPoint == null || route.TransitionPoint == null ||
                route.CanKuneKuneUse == false)
                continue;

            routes.Add(route);
        }

        return routes;
    }

    private int FindNearestNodeIndex(List<RouteSearchNode> nodes)
    {
        int nearestIndex = 0;
        float nearestDistance = nodes[0].Distance;

        for (int i = 1; i < nodes.Count; i++)
        {
            if (nodes[i].Distance >= nearestDistance)
                continue;

            nearestIndex = i;
            nearestDistance = nodes[i].Distance;
        }

        return nearestIndex;
    }
}
