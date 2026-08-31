using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현재 Map 정보와 Map 이동을 관리하는 Singlton Class.
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
        QuestManager.Instantiate();
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
    /// 플레이어의 위치를 targetMap의 targetEntryPoint 위치로 이동시키는 메서드.
    /// </summary>
    /// <param name="targetMap">이동할 Map</param>
    /// <param name="targetEntryPoint">이동할 위치</param>
    public void ChangeMap(MapArea targetMap, EntryPoint targetEntryPoint)
    {
        if (targetMap == null || targetEntryPoint == null) return;
        
        SetCurrentMap(targetMap);
        player.transform.position = targetEntryPoint.SpawnPoint.position;
        CameraManager.Instance.SetFollowCamera(targetEntryPoint.MapCamera);
    }

    /// <summary>
    /// 쿠네쿠네의 현재 위치에서 플레이어까지 이어지는 문 경로 중 이동 거리가 가장 짧은 경로를 찾습니다. (다익스트라)
    /// </summary>
    /// <param name="startMap">쿠네쿠네의 현재 맵</param>
    /// <param name="startPosition">쿠네쿠네 현재 위치</param>
    /// <param name="targetMap">플레이어가 있는 맵</param>
    /// <param name="targetPosition">플레이어 위치</param>
    /// <returns></returns>
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
            if (arrivalEntryPoint == null || arrivalEntryPoint.SpawnPoint == null)
                continue;

            Vector3 spawnPosition = arrivalEntryPoint.SpawnPoint.position;

            // 플레이어가 있는 맵 루트를 찾았다
            if (currentNode.Route.TargetMap == targetMap)
            {
                // 쿠네쿠네의 현재 위치에서 여러 문을 거쳐 플레이어가 있는 맵에 들어가기까지의 거리 + 플레이어와 문 거리
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
                // 쿠네쿠네의 현재 위치에서 여러 문을 거쳐 플레이어가 있는 맵에 들어가기까지의 거리 + 다음으로 찾을 문과 현재 열고 들어온 문 거리
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

    /// <summary>
    /// 마지막 문부터 이전 문을 거꾸로 따라간 뒤, 실제 이동 순서로 정렬하여 반환합니다.
    /// </summary>
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

    /// <summary>
    /// 지정한 맵에서 다른 맵으로 이어지는 문과 출구를 모두 찾습니다.
    /// </summary>
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

    /// <summary>
    /// 아직 확인하지 않은 경로 중 누적 이동 거리가 가장 짧은 경로의 위치를 찾습니다.
    /// </summary>
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
