using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Ending0CutsceneController : CutsceneControllerBase
{
    protected override void EndCutscene()
    {
        StartCoroutine(EndingRoutine());
    }

    private IEnumerator EndingRoutine()
    {
        if (UIManager.Instance != null) yield return UIManager.Instance.FadeOut(1.5f);

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene("Scene_Intro");
    }
}
