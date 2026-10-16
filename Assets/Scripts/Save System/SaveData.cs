using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    [Header("Chapter Info")]
    public EChapter currentChapter = EChapter.Chapter1_Outside;

    [Header("Checkpoint Info")]
    public EMapType respawnMapType;
    // 비어 있으면 기존 세이브와 동일하게 챕터 시작 위치에서 재개합니다.
    public string respawnEntryPointID;
    // 지정되어 있으면 입구 대신 해당 맵 기준의 좌표에서 재개합니다.
    public bool hasCheckpointPosition;
    public Vector3 checkpointLocalPosition;
    public bool hasCheckpointLighting;
    public Color checkpointLightColor = Color.white;
    public float checkpointLightIntensity = 1f;

    [Header("Player State")]
    public EPlayerAge playerAge;
    public float currentHeat;

    [Header("Event Flags (진행도 기억)")]
    public List<string> eventKeys = new List<string>();
    public List<bool> eventValues = new List<bool>();

    [Header("Inventory Data")]
    public List<string> keyItemIDs = new List<string>();        // 중요 아이템(열쇠 등)
    public List<string> consumableItemIDs = new List<string>(); // 소모품 종류
    public List<int> consumableItemCounts = new List<int>();    // 소모품 개수

    [Header("Quest Data")]
    public List<string> completedQuestIDs = new List<string>();
    public List<string> activeQuestIDs = new List<string>();
    public List<int> activeQuestProgress = new List<int>(); // 각 퀘스트의 현재 진행 단계

    public void SetFlag(string key, bool value)
    {
        int index = eventKeys.IndexOf(key);
        if (index >= 0)
        {
            eventValues[index] = value;
        }
        else
        {
            eventKeys.Add(key);
            eventValues.Add(value);
        }
    }

    public bool GetFlag(string key, bool defalutValue = false)
    {
        int index = eventKeys.IndexOf(key);
        return index >= 0 ? eventValues[index] : defalutValue;
    }
}
