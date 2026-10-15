using UnityEngine;

public class KunekuneHead : MonoBehaviour, IInteractable
{
    private RisingScareImage scareImage;
    private bool isInteracted = false;

    public void Setup(Transform targetDropPoint, RisingScareImage targetScareImage)
    {
        scareImage = targetScareImage;
        transform.position = targetDropPoint.position;
        gameObject.SetActive(true);
    }

    public void OnInteractPressed()
    {
        if (isInteracted) return;

        isInteracted = true;

        if (scareImage == null)
        {
            Destroy(gameObject);
            return;
        }

        scareImage.Play(() => Destroy(gameObject));
    }
}
