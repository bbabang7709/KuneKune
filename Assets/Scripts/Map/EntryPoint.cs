using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 맵 이동 시 스폰될 위치(Transform SpawnPoint)와 카메라 정보(CinemachineCamera MapCamera)를 담는 클래스
/// </summary>
public class EntryPoint : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;

    [SerializeField] private CinemachineCamera mapCamera;

    public Transform SpawnPoint => spawnPoint;
    public CinemachineCamera MapCamera => mapCamera;
}
