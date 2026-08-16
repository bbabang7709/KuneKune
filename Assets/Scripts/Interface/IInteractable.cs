
using UnityEngine;

/// <summary>
/// Interact 가능한 물체들의 공통 기능을 담은 Interface
/// </summary>
public interface IInteractable
{
    Transform PromptPoint { get; }
    
    void OnInteractPressed();
    void OnInteractHeld();
    void OnInteractReleased();
}
