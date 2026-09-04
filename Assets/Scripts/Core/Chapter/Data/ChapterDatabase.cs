using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ChapterStartInfo
{
    public EChapter chapter;
    public EPlayerAge playerAge;
    public EMapType startMapType;
    public string startEntryPointID;
}

[CreateAssetMenu(fileName = "ChapterDatabase", menuName = "KuneKune/Chapter/ChapterDatabase")]
public class ChapterDatabase : ScriptableObject
{
    [Tooltip("각 챕터별 시작 정보를 세팅합니다.")]
    public List<ChapterStartInfo> chapters = new List<ChapterStartInfo>();

    public ChapterStartInfo GetChapterInfo(EChapter chapter)
    {
        foreach (var info in chapters)
        {
            if (info.chapter == chapter)
            {
                return info;
            }
        }

        Debug.LogError($"[ChapterDatabase] {chapter} 에 해당하는 데이터가 없습니다.");
        return default;
    }
}
