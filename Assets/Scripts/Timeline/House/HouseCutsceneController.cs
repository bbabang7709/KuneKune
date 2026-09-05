using UnityEngine;

public class HouseCutsceneController : CutsceneControllerBase
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayCutscene();
        }
    }
    protected override void OnCutsceneEndSetup()
    {
        QuestManager.Instance?.StartQuest("HouseCleanup");
        SaveManager.Instance?.SaveChapterCheckpoint(EChapter.Chapter2_House);
    }
}
