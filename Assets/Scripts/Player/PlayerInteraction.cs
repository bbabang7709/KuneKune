using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private List<IInteractable> nearbyInteractables = new List<IInteractable>();
    private IInteractable currentTarget = null;
    public IInteractable CurrentTarget => currentTarget;

    void Update()
    {
        UpdateCurrentTarget();
    }

    private void UpdateCurrentTarget()
    {
        if (nearbyInteractables.Count == 0)
        {
            if (currentTarget != null) ClearTarget();
            return;
        }

        IInteractable closest = null;
        float closestDistance = Mathf.Infinity;
        Vector2 playerPos = transform.position;

        foreach (var interactable in nearbyInteractables)
        {
            if (interactable is MonoBehaviour mono)
            {
                float distance = Vector2.Distance(playerPos, mono.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = interactable;
                }
            }
        }

        if (currentTarget != closest)
        {
            currentTarget = closest;
        }
    }

    private void ClearTarget()
    {
        currentTarget = null;
        Debug.Log("상호작용 타겟 없음");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<IInteractable>(out var interactable))
        {
            nearbyInteractables.Add(interactable);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent<IInteractable>(out var interactable))
        {
            nearbyInteractables.Remove(interactable);
        }
    }
}
