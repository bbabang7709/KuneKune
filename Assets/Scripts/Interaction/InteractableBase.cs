using UnityEngine;

public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    private Transform promptPoint;
    public Transform PromptPoint
    {
        get
        {
            if (promptPoint == null)
            {
                promptPoint = transform.Find("Prompt Point");
                if (promptPoint == null)
                {
                    promptPoint = transform;
                }
            }

            return promptPoint;
        }
    }

    public abstract void OnInteractPressed();
}
