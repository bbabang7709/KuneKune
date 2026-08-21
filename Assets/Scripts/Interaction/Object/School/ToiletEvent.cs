using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToiletEvent : MonoBehaviour
{
    [Header("Dialog Data")]
    [SerializeField] private DialogData interactionDialog;
    [SerializeField] private DialogData finalInteractionDialog;

    [Header("Event Objects")]
    [SerializeField] private List<ToiletBowl> toiletBowls;
    [SerializeField] private KunekuneHead kunekuneHeadPrefab;
    [SerializeField] private GameObject classroomKey;

    private static int MAX_COUNT = 4;
    private int interactCount = 0;

    public void PlusCount()
    {
        if (interactCount >= MAX_COUNT) return;

        interactCount++;
        Debug.Log($"현재 상호작용 횟수: {interactCount}");

        if (interactCount == MAX_COUNT)
        {
            StartCoroutine(FinalEventSequence());
        }
        else
        {
            Interact();
        }
    }
    
    private void Interact()
    {
        DialogSystem.Instance?.PlayDialog(interactionDialog);
    }

    private IEnumerator FinalEventSequence()
    {
        DialogSystem.Instance?.PlayDialog(finalInteractionDialog);

        if (DialogSystem.Instance != null)
        {
            yield return new WaitWhile(() => DialogSystem.Instance.IsDialogPlaying);
        }

        int randomIndex = UnityEngine.Random.Range(0, toiletBowls.Count);
        ToiletBowl targetBowl = toiletBowls[randomIndex];

        Debug.Log($"{targetBowl.name} 위치에 쿠네쿠네 머리 등장!");

        KunekuneHead headInstance = Instantiate(kunekuneHeadPrefab, targetBowl.DropPoint.position, Quaternion.identity);
        headInstance.Setup(targetBowl.DropPoint, classroomKey);
    }
}
