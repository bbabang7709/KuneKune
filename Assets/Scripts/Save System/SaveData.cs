using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public int currentMapType;
    public Vector3 playerPosition;
    public int playerAge;

    public Dictionary<string, bool> eventFlags = new();
}
