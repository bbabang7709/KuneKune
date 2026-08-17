using UnityEngine;

public class ToiletBowl : MonoBehaviour, IInteractable
{
    [Header("Prompt Settings")]
    [SerializeField] private Transform promptPoint;
    public Transform PromptPoint => promptPoint;

    public void OnInteractPressed()
    {
        Debug.Log($"{name} 상호작용");
    }

    public void OnInteractHeld() {}
    public void OnInteractReleased() {}
}
