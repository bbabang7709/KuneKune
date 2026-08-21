using System;
using UnityEngine;

public class ToiletBowl : MonoBehaviour, IInteractable
{
    private Transform dropPoint;
    public Transform DropPoint
    {
        get
        {
            if (dropPoint == null)
            {
                dropPoint = transform.Find("Drop Point");
            }

            return dropPoint;
        }
    }
    private ToiletEvent toiletEvent;

    void Awake()
    {
        toiletEvent = GetComponentInParent<ToiletEvent>();
    }

    public void OnInteractPressed()
    {
        Debug.Log($"{name} 상호작용");
        toiletEvent.PlusCount();

        if (TryGetComponent<Collider2D>(out var bowlCollider))
        {
            bowlCollider.enabled = false;
        }

        enabled = false;
    }    
}
