using System.Collections;
using UnityEngine;

public class FallCutsceneController : CutsceneControllerBase
{
    private IEnumerator WaitAndPlayCutscene()
    {
        if (SaveManager.Instance != null)
        {
            yield return new WaitUntil(() => SaveManager.Instance.IsLoading == false);
        }

        PlayCutscene();
    }
}
