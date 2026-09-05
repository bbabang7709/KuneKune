using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public EChapter currentChapter = EChapter.Chapter1_Outside;

    [Header("Checkpoint Info")]
    public EMapType respawnMapType = EMapType.Outside_Busstop;
    public string respawnEntryPointID = "outside_gamestart";

    [Header("Player State")]
    public EPlayerAge playerAge;
    public float currentHeat;

    [Header("Event Flags (진행도 기억)")]
    public List<string> eventKeys = new List<string>();
    public List<bool> eventValues = new List<bool>();

    [Header("Inventory (인벤토리)")]
    public List<string> keyItemIDs = new List<string>();        // 중요 아이템(열쇠 등)
    public List<string> consumableItemIDs = new List<string>(); // 소모품 종류
    public List<int> consumableItemCounts = new List<int>();    // 소모품 개수

    public void SaveEventFlag(string key, bool value)
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

    public bool GetEventFlag(string key, bool defalutValue = false)
    {
        int index = eventKeys.IndexOf(key);
        if (index >= 0)
        {
            return eventValues[index];
        }
        
        return defalutValue;
    }
}
