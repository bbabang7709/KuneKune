using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    [Header("Checkpoint Info")]
    public EMapType respawnMapType = EMapType.Outside_Busstop;
    public string respawnEntryPointID = "outside_gamestart";

    [Header("Player State")]
    public EPlayerAge playerAge;

    public List<string> eventKeys = new List<string>();
    public List<bool> eventValues = new List<bool>();

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
