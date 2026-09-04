using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    
    private SaveData _currentSaveData = new SaveData();
    private List<ISaveable> _saveableObjects = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Register(ISaveable saveable) => _saveableObjects.Add(saveable);
    public void Unregister(ISaveable saveable) => _saveableObjects.Remove(saveable);

    public void AutoSave()
    {
        _currentSaveData.currentMapType = (int)MapManager.Instance.CurrentMap.MapType;

        foreach (var obj in _saveableObjects)
        {
            obj.SaveState(_currentSaveData);
        }

        string json = JsonUtility.ToJson(_currentSaveData);
        System.IO.File.WriteAllText(Application.persistentDataPath + "/autosave.json", json);
        Debug.Log("Auto Saved!");
    }

    public void LoadGame()
    {
        
    }
}
