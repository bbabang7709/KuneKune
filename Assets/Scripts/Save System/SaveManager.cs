using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    public const string TrueEndingUnlockedFlag = "HasUnlockedTrueEnding";

    public bool IsTrueEndingUnlocked => GetFlag(TrueEndingUnlockedFlag);

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

    /// <summary>
    /// 진엔딩 조건을 달성했음을 기록하고 현재 진행도를 즉시 저장합니다.
    /// </summary>
    public void UnlockTrueEnding()
    {
        if (IsLoading || IsTrueEndingUnlocked) return;

        SetFlag(TrueEndingUnlockedFlag, true);
        SaveCurrentProgress();
    }

    /// <summary>
    /// 현재 챕터 체크포인트를 유지하며 이벤트 플래그와 플레이어 진행도를 저장합니다.
    /// </summary>
    public void SaveCurrentProgress()
    {
        if (IsLoading) return;

        foreach (var obj in _saveableObjects)
        {
            obj.SaveState(_currentSaveData);
        }

        string json = JsonUtility.ToJson(_currentSaveData, true);
        File.WriteAllText(SavePath, json);
    }

    public void SaveChapterCheckpoint(EChapter newChapter)
    {
        if (chapterDatabase == null) return;

        ChapterStartInfo info = chapterDatabase.GetChapterInfo(newChapter);

        _currentSaveData.currentChapter = info.chapter;
        _currentSaveData.playerAge = info.playerAge;

        SaveCurrentProgress();
        Debug.Log($"[SaveManager] {newChapter} 체크포인트 저장 완료");
    }


    public void Load(bool skipFadeOut = false)
    {
        if (!HasSaveFile())
        {
            Debug.LogWarning("[SaveManager] 불러올 세이브 파일이 없습니다.");
            return;
        }

        if (IsLoading) return;

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

        UnityEngine.Events.UnityAction<Scene, LoadSceneMode> onSceneLoaded = (scene, mode) => 
        {
            if (UIManager.Instance != null) 
            {
                UIManager.Instance.SetFadeAlpha(1f);
                UIManager.Instance.SetGameplayUIAlpha(0f);
            }

            if (ControlLockManager.Instance != null)
            {
                ControlLockManager.Instance.ClearAllLocks();
                ControlLockManager.Instance.AddInputLock(this);
                ControlLockManager.Instance.AddMovementLock(this);
            }
        };
        SceneManager.sceneLoaded += onSceneLoaded;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        yield return new WaitUntil(() => asyncLoad.isDone);

        SceneManager.sceneLoaded -= onSceneLoaded;

        yield return null; 
        _saveableObjects.RemoveWhere(obj => obj == null || (obj as MonoBehaviour) == null);

        try
        {
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
        } 
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 세이브 로드 중 치명적 에러 발생!!!! 코루틴 즉사를 방지했습니다.\n {e}");
        }
        
        if (BackgroundManager.Instance != null)
            BackgroundManager.Instance.SetBackgroundForChapter(_currentSaveData.currentChapter);

        yield return new WaitForSecondsRealtime(1.0f);

        if (UIManager.Instance != null && UIManager.Instance.gameObject.activeInHierarchy) 
        {
            UIManager.Instance.StartCoroutine(UIManager.Instance.FadeIn(0.5f));
            UIManager.Instance.StartCoroutine(UIManager.Instance.FadeInGameplayUI(0.5f));

            yield return new WaitForSecondsRealtime(1f);
        }

        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.RemoveInputLock(this);
            ControlLockManager.Instance.RemoveMovementLock(this);
            ControlLockManager.Instance.ClearAllLocks();
        }
        
        IsLoading = false;
    }
}
