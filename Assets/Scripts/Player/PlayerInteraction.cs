using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private InteractionPromptUI promptUI;
    private List<IInteractable> nearbyInteractables = new List<IInteractable>();
    private IInteractable currentTarget = null;
    public IInteractable CurrentTarget => currentTarget;

    void Update()
    {
        UpdateCurrentTarget();
        UpdateInteractionUI();
    }

    private void UpdateCurrentTarget()
    {
        if (nearbyInteractables.Count == 0)
        {
            if (currentTarget != null) 
                ClearTarget();

            return;
        }

        IInteractable closest = null;
        float closestDistance = Mathf.Infinity;

        Vector2 playerPos = transform.position;

        foreach (var interactable in nearbyInteractables)
        {
            if (interactable is not MonoBehaviour mono) continue;
            if (!mono.gameObject.activeInHierarchy) continue;   // 비활성화된 오브젝트 제외

            float distance = Vector2.Distance(playerPos, mono.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = interactable;
            }
        }

        if (currentTarget != closest)
        {
            currentTarget = closest;
        }
    }

    private void ClearTarget()
    {
        if (currentTarget != null)
        {
            currentTarget.OnInteractReleased();
        }

        currentTarget = null;

        promptUI?.Hide();

        Debug.Log("상호작용 타겟 없음");
    }

    private void UpdateInteractionUI()
    {
        if (promptUI == null) return;
        
        if (currentTarget == null)
        {
            promptUI.Hide();
            return;
        }

        promptUI.SetPosition(currentTarget.PromptPoint.position);

        if (currentTarget is IHoldInteractable holdInteractable)
        {
            promptUI.ShowHold(holdInteractable.HoldProgress);
        }
        else
        {
            promptUI.Show();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<IInteractable>(out var interactable))
        {
            if (!nearbyInteractables.Contains(interactable))
            {
                nearbyInteractables.Add(interactable);    
            }      
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent<IInteractable>(out var interactable))
        {
            nearbyInteractables.Remove(interactable);

            if (currentTarget == interactable)
            {
                ClearTarget();
            }
        }
    }
}
