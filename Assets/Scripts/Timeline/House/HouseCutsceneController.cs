using System.Collections;
using UnityEngine;

public class HouseCutsceneController : CutsceneControllerBase
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            StartCoroutine(WaitAndPlayCutscene());
        }
    }
    protected override void OnCutsceneEndSetup()
    {
        SaveManager.Instance?.SaveChapterCheckpoint(EChapter.Chapter2_House);
        QuestManager.Instance?.StartQuest("HouseCleanup");
    }

    private IEnumerator WaitAndPlayCutscene()
    {
        if (SaveManager.Instance != null)
        {
            yield return new WaitUntil(() => SaveManager.Instance.IsLoading == false);
        }

        PlayCutscene();
    }
}
