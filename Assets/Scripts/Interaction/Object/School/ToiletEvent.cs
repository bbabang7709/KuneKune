using UnityEngine;

public class ToiletEvent : MonoBehaviour
{
    [Header("Dialog Data")]
    [SerializeField] private DialogData interactionDialog;

    private static int MAX_COUNT = 4;
    private int interactCount = 0;

    public void PlusCount()
    {
        if (interactCount >= MAX_COUNT) return;

        interactCount++;
        Debug.Log($"현재 상호작용 횟수: {interactCount}");

        // 모두 조사해도 추가 오브젝트나 보상 없이 일반 조사로 끝냅니다.
        Interact();
    }
    
    private void Interact()
    {
        DialogSystem.Instance?.PlayDialog(interactionDialog);
    }
}
