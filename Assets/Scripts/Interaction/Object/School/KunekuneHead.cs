using System.Collections;
using UnityEngine;

public class KunekuneHead : MonoBehaviour, IInteractable
{
    [Header("Jump Scare Resources")] 
    [SerializeField] private Sprite jumpScareIllustration;

    private Transform dropPoint;
    private GameObject classroomKey;
    private bool isInteracted = false;

    public void Setup(Transform targetDropPoint, GameObject key)
    {
        dropPoint = targetDropPoint;
        classroomKey = key;
        gameObject.SetActive(true);
    }   

    public void OnInteractPressed()
    {
        if (isInteracted) return;

        isInteracted = true;
        StartCoroutine(JumpScareRoutine());
    }

    private IEnumerator JumpScareRoutine()
    {
        // TODO: 사운드 연출

        if (jumpScareIllustration != null && DialogSystem.Instance != null)
        {
            bool dialogFinished = false;

            DialogSystem.Instance.PlayFullScreenImageDialog(
                jumpScareIllustration, 
                null, 
                () => dialogFinished = true
            );

            yield return new WaitUntil(() => dialogFinished);

            if (classroomKey != null && dropPoint != null)
            {
                classroomKey.transform.position = dropPoint.position;
                classroomKey.SetActive(true);
            }

            Destroy(gameObject);
        }
    }
}
