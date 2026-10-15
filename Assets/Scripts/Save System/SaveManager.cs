using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    public const string TrueEndingUnlockedFlag = "HasUnlockedTrueEnding";

    public bool IsTrueEndingUnlocked
    {
        get
        {
#if UNITY_EDITOR
            if (_trueEndingChaseTestOwner != null)
                return true;
#endif
            return GetFlag(TrueEndingUnlockedFlag);
        }
    }

#if UNITY_EDITOR
    // 테스트 통로가 씬 전환으로 파괴되면 기존 진엔딩 조건으로 돌아갑니다.
    [System.NonSerialized] private Object _trueEndingChaseTestOwner;

    public void EnableTrueEndingChaseTest(Object owner)
    {
        if (IsLoading || owner == null) return;
        _trueEndingChaseTestOwner = owner;
    }
#endif

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

    /// <summary>새 회차의 아이템, 컷씬, 이벤트와 퀘스트 진행 기록을 비웁니다.</summary>
    public void ResetForNewGame()
    {
        if (IsLoading) return;

        _currentSaveData = new SaveData();
#if UNITY_EDITOR
        _trueEndingChaseTestOwner = null;
#endif
    }

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
        if (IsLoading) return;

        if (!HasSaveFile())
        {
            Debug.LogWarning("[SaveManager] 불러올 세이브 파일이 없습니다.");

            // 사망 연출처럼 이미 암전된 상태로 호출됐다면 씬을 다시 불러와 암전에서 빠져나옵니다.
            if (!skipFadeOut) return;
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

        // 암전과 씬 재로드 동안 쿠네쿠네가 플레이어를 덮치지 못하게 먼저 치웁니다.
        MapManager.Instance?.KuneKune?.Despawn();

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
            if (!HasSaveFile())
                throw new FileNotFoundException("세이브 파일이 없어 씬만 다시 불러옵니다.", SavePath);

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
        
        try
        {
            if (BackgroundManager.Instance != null)
                BackgroundManager.Instance.SetBackgroundForChapter(_currentSaveData.currentChapter);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 배경 복원 중 에러가 발생했습니다.{e}");
        }

        yield return new WaitForSecondsRealtime(1.0f);

        // 어떤 경우에도 암전 상태로 남지 않도록 페이드 인을 보장합니다.
        if (UIManager.Instance != null)
        {
            if (UIManager.Instance.gameObject.activeInHierarchy)
            {
                UIManager.Instance.StartCoroutine(UIManager.Instance.FadeIn(0.5f));
                UIManager.Instance.StartCoroutine(UIManager.Instance.FadeInGameplayUI(0.5f));

                yield return new WaitForSecondsRealtime(1f);
            }
            else
            {
                UIManager.Instance.SetFadeAlpha(0f);
                UIManager.Instance.SetGameplayUIAlpha(1f);
            }
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
