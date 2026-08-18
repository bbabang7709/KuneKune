using UnityEngine;

/// <summary>
/// 상호작용(F키)을 통해 맵 전환을 유발하는 Interactable
/// </summary>
public class Door : MapTransition, IInteractable
{
    [Header("Prompt Settings")]
    [SerializeField] private Transform promptPoint;

    [Header("Lock State")]
    [SerializeField] private bool isLocked = false;

    public Transform PromptPoint => promptPoint;
    private GameObject user;

    public void OnInteractPressed()
    {
        if (user == null) return;

        if (!isLocked) 
        {
            ExecuteTransition();
        }
        else
        {
            // TODO: Text 연출 추가
            Debug.Log($"{name}: 잠겨 있음.");
            return;
        }
    }
    public void OnInteractHeld() {}
    public void OnInteractReleased() {}

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            user = collision.gameObject;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            user = null;
        }
    }
}
