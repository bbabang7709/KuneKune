using UnityEngine;

public class SimpleInteractable : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    [SerializeField] private Transform promptPoint;
    public Transform PromptPoint => promptPoint;

    [Header("Dialog")]
    [SerializeField] private DialogData dialog;
    public void OnInteractPressed()
    {
        if (dialog == null) return;

        UIManager.Instance?.PlayDialog(dialog);
    }

    public void OnInteractHeld() {}
    public void OnInteractReleased() {}
}
