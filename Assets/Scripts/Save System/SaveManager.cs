using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Database")]
    [SerializeField] private ChapterDatabase chapterDatabase;
    
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

    public bool HasSaveFile()
    {
        return File.Exists(SavePath);
    }

    public void SaveChapterCheckpoint(EChapter newChapter)
    {
        if (chapterDatabase == null) return;

        ChapterStartInfo info = chapterDatabase.GetChapterInfo(newChapter);

        _currentSaveData.currentChapter = info.chapter;
        _currentSaveData.respawnMapType = info.startMapType;
        _currentSaveData.respawnEntryPointID = info.startEntryPointID;
        _currentSaveData.playerAge = info.playerAge;

        foreach (var obj in _saveableObjects)
        {
            obj.SaveState(_currentSaveData);
        }

        string json = JsonUtility.ToJson(_currentSaveData, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[SaveManager] {newChapter} 체크포인트 저장 완료");
    }


    public void Load()
    {
        if (!HasSaveFile())
        {
            Debug.LogWarning("[SaveManager] 불러올 세이브 파일이 없습니다.");
            return;
        }

        Time.timeScale = 1f;
        StartCoroutine(LoadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.AddInputLock(this);
            ControlLockManager.Instance.AddMovementLock(this);
        }

        if (UIManager.Instance != null)
        {
            yield return UIManager.Instance.FadeOut(0.5f);
        }

        // AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);

        // --- 여기서부터는 씬이 새로 태어난 직후. (Manager들이 새로 Awake) --- //

        string json = File.ReadAllText(SavePath);
        _currentSaveData = JsonUtility.FromJson<SaveData>(json);

        if (chapterDatabase != null)
        {
            ChapterStartInfo info = chapterDatabase.GetChapterInfo(_currentSaveData.currentChapter);

            if (MapManager.Instance != null)
            {
                MapManager.Instance.InitMap(info.startMapType, info.startEntryPointID);
            }
        }

        foreach (var obj in _saveableObjects)
        {
            obj.LoadState(_currentSaveData);
        }

        Debug.Log("[SaveManager] 데이터 로드 완료");

        if (UIManager.Instance != null)
        {
            yield return UIManager.Instance.FadeIn(0.5f);
        }

        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.RemoveInputLock(this);
            ControlLockManager.Instance.RemoveMovementLock(this);
        }
    }
}
