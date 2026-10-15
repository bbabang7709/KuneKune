using UnityEngine;

public class ToiletEvent : MonoBehaviour
{
    [Header("Dialog Data")]
    [SerializeField] private DialogData interactionDialog;
    [SerializeField] private DialogData eventDialog;

    [Header("Scare")]
    [SerializeField] private KunekuneHead kunekuneHeadPrefab;
    [SerializeField] private RisingScareImage scareImage;

    private static int MAX_COUNT = 4;
    private int interactCount = 0;

    public void PlusCount()
    {
        if (interactCount >= MAX_COUNT) return;

        interactCount++;
        Debug.Log($"현재 상호작용 횟수: {interactCount}");

        if (interactCount < MAX_COUNT)
        {
            Interact();
        }
        else
        {
            ExecuteEvent();
        }
    }
    
    private void Interact()
    {
        DialogSystem.Instance?.PlayDialog(interactionDialog);
    }

    private void ExecuteEvent()
    {
        DialogSystem.Instance?.PlayDialog(eventDialog, SpawnKunekuneHead);
    }

    // 4개의 변기 중 하나를 무작위로 골라 그 위치에 쿠네쿠네 머리를 생성한다.
    private void SpawnKunekuneHead()
    {
        if (kunekuneHeadPrefab == null) return;

        ToiletBowl[] bowls = GetComponentsInChildren<ToiletBowl>(true);
        if (bowls.Length == 0) return;

        ToiletBowl chosen = bowls[Random.Range(0, bowls.Length)];
        Transform dropPoint = chosen.DropPoint;
        if (dropPoint == null) return;

        Debug.Log($"쿠네쿠네 머리 생성 위치: {chosen.name} ({bowls.Length}개 중 선택)");

        KunekuneHead head = Instantiate(kunekuneHeadPrefab, dropPoint.position, Quaternion.identity, transform);
        head.Setup(dropPoint, scareImage);
    }
}
