using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    
    private SaveData _currentSaveData = new SaveData();
    private HashSet<ISaveable> _saveableObjects = new HashSet<ISaveable>();

    private string SavePath => Application.persistentDataPath + "/kunekune_save.json";

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
        foreach (var obj in _saveableObjects)
        {
            obj.SaveState(_currentSaveData);
        }

        string json = JsonUtility.ToJson(_currentSaveData, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[SaveManager] 자동 저장 완료: {SavePath}");
    }

    public void LoadGame()
    {
        if (!File.Exists(SavePath))
        {
            Debug.LogWarning("[SaveManager] 세이브 파일이 없습니다.");
            return;
        }

        string json = File.ReadAllText(SavePath);
        _currentSaveData = JsonUtility.FromJson<SaveData>(json);

        foreach (var obj in _saveableObjects)
        {
            obj.LoadState(_currentSaveData);
        }

        Debug.Log("[SaveManager] 로드 완료");
    }
}
