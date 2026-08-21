using UnityEngine;

public class SimpleInteractable : InteractableBase
{
    [Header("Dialog")]
    [SerializeField] private DialogData dialog;

    public override void OnInteractPressed()
    {
        if (dialog == null) return;
        DialogSystem.Instance?.PlayDialog(dialog);
    }
}
