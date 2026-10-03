using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;

public enum EMapType
{
    Outside_Busstop,
    Outside_Ricefield,
    Outside_School,
    Outside_House,
    House_Hallway,
    House_Bedroom,
    House_Toilet,
    House_2F,
    House_MyRoom,
    School_Hallway,
    School_Classroom,
    School_ManToilet,
    School_WomanToilet,
    School_2F,
    SchoolArtroom
}

/// <summary>
/// Map의 정보를 담는 클래스 (EMapType + MapID)
/// </summary>
public class MapArea : MonoBehaviour
{
    [Header("Map Info")]
    public EMapType mapType;

    [Header("Camera Setting")]
    [Tooltip("이 맵 전용 Cinemachine Camera")]
    [SerializeField] private CinemachineCamera _areaCamera;

    [Header("BGM ID")]
    [Tooltip("이 맵에서 재생할 BGM ID 입력")]
    public string BgmID;
    public bool IsInside = true;
    
    [Header("KuneKune_ables")]
    [Tooltip("이 맵에 등록된 kunekune_ables 연결")]
    [SerializeField] private Transform _kuneKune_ables;
   
    public CinemachineCamera AreaCamera => _areaCamera;
    public Transform KuneKune_ables => _kuneKune_ables;

    private Dictionary<string, Transform> _entryPointDict = new Dictionary<string, Transform>();

    void Awake()
    {
        EntryPoint[] points = GetComponentsInChildren<EntryPoint>(true);
        foreach (var point in points)
        {
            if (!string.IsNullOrEmpty(point.EntryID) && !_entryPointDict.ContainsKey(point.EntryID))
            {
                _entryPointDict.Add(point.EntryID, point.transform);
            }
        }
    }

    public void ActivateArea()
    {
        gameObject.SetActive(true);
    }

    public void DeactivateArea()
    {
        gameObject.SetActive(false);
    }

    public Transform GetEntryPoint(string entryID)
    {
        if (_entryPointDict.TryGetValue(entryID, out Transform pointTransform))
        {
            return pointTransform;
        }

        Debug.LogWarning($"[{mapType}] 맵에서 EntryPoint '{entryID}를 찾을 수 없습니다.'");
        return null;
    }
}
