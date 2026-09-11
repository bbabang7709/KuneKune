using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public bool IsLoading { get; private set; }

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

    public bool HasSaveFile() => File.Exists(SavePath);

    public void SetFlag(string uniqueID, bool isCompleted) => _currentSaveData.SetFlag(uniqueID, isCompleted);
    public bool GetFlag(string uniqueID, bool defaultValue = false) => _currentSaveData.GetFlag(uniqueID, defaultValue);

    public void SaveChapterCheckpoint(EChapter newChapter)
    {
        if (chapterDatabase == null) return;

        ChapterStartInfo info = chapterDatabase.GetChapterInfo(newChapter);

        _currentSaveData.currentChapter = info.chapter;
        _currentSaveData.playerAge = info.playerAge;

        foreach (var obj in _saveableObjects)
        {
            obj.SaveState(_currentSaveData);
        }

        string json = JsonUtility.ToJson(_currentSaveData, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[SaveManager] {newChapter} 체크포인트 저장 완료");
    }


    public void Load(bool skipFadeOut = false)
    {
        if (!HasSaveFile())
        {
            Debug.LogWarning("[SaveManager] 불러올 세이브 파일이 없습니다.");
            return;
        }

        Time.timeScale = 1f;
        StartCoroutine(LoadRoutine(skipFadeOut));
    }

    private IEnumerator LoadRoutine(bool skipFadeOut)
    {
        IsLoading = true;

        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.AddInputLock(this);
            ControlLockManager.Instance.AddMovementLock(this);
        }

        if (!skipFadeOut && UIManager.Instance != null) yield return UIManager.Instance.FadeOut(1.0f);

        // =====================================================================
        // 💡 핵심 픽스: 씬이 로드되자마자 첫 프레임이 그려지기 전에 화면을 덮는 이벤트를 예약합니다.
        UnityEngine.Events.UnityAction<Scene, LoadSceneMode> onSceneLoaded = (scene, mode) => 
        {
            if (UIManager.Instance != null) 
            {
                UIManager.Instance.SetFadeAlpha(1f);
                UIManager.Instance.SetGameplayUIAlpha(0f);
            }

            if (ControlLockManager.Instance != null)
            {
                ControlLockManager.Instance.AddInputLock(this);
                ControlLockManager.Instance.AddMovementLock(this);
            }
        };
        SceneManager.sceneLoaded += onSceneLoaded;
        // =====================================================================

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        yield return new WaitUntil(() => asyncLoad.isDone);

        // =====================================================================
        // 💡 화면을 덮었으므로 예약했던 이벤트를 안전하게 해제합니다.
        SceneManager.sceneLoaded -= onSceneLoaded;
        // =====================================================================

        yield return null; 
        _saveableObjects.RemoveWhere(obj => obj == null || (obj as MonoBehaviour) == null);

        string json = File.ReadAllText(SavePath);
        _currentSaveData = JsonUtility.FromJson<SaveData>(json);

        if (chapterDatabase != null)
        {
            ChapterStartInfo info = chapterDatabase.GetChapterInfo(_currentSaveData.currentChapter);
            if (MapManager.Instance != null) MapManager.Instance.InitMap(info.startMapType, info.startEntryPointID);
        }

        foreach (var obj in _saveableObjects)
        {
            obj.LoadState(_currentSaveData);
        }

        yield return new WaitForSeconds(1.0f);

        if (UIManager.Instance != null) 
        {
            yield return UIManager.Instance.FadeIn(1.0f);

            yield return UIManager.Instance.FadeInGameplayUI(1.0f);
        }

        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.RemoveInputLock(this);
            ControlLockManager.Instance.RemoveMovementLock(this);
        }
        
        IsLoading = false;
    }
}
