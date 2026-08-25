using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 주변의 상호작용 가능한 객체를 관리합니다.
/// 
/// 주변의 IInteractable을 탐색하여 가장 가까운 객체를
/// 현재 상호작용 대상으로 설정하고,
/// PlayerController로부터 전달받은 상호작용 명령을
/// 해당 객체에 전달합니다.
/// 
/// Hold 상호작용이 시작되면 현재 대상을 ActiveTarget으로 고정하여
/// 플레이어가 이동하더라도 다른 상호작용 대상으로 변경되지 않도록 합니다.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private InteractionPromptUI promptUI;

    [Header("Interaction")]
    [SerializeField]
    private float interactionDistance = 1.5f;

    private const string PromptPointName = "Prompt Point";

    private readonly List<IInteractable> nearbyInteractables = new();

    /// <summary>
    /// 현재 플레이어와 가장 가까운 상호작용 대상입니다.
    /// Hold 상호작용 중이 아닐 때 변경될 수 있습니다.
    /// </summary>
    private IInteractable currentTarget;

    /// <summary>
    /// 현재 진행 중인 상호작용 대상입니다.
    /// Hold 상호작용이 시작되면 해당 대상을 고정합니다.
    /// </summary>
    private IInteractable activeTarget;

    /// <summary>
    /// 현재 플레이어가 선택한 상호작용 대상입니다.
    /// </summary>
    public IInteractable CurrentTarget => currentTarget;

    /// <summary>
    /// 현재 Hold 상호작용을 진행 중인지 여부입니다.
    /// </summary>
    public bool IsHolding =>
        activeTarget is IHoldInteractable;

    /// <summary>
    /// 현재 상호작용이 진행 중인지 여부입니다.
    /// </summary>
    public bool IsInteracting =>
        activeTarget != null;

    private void Update()
    {
        // 상호작용 진행 중에는 현재 타겟을 변경하지 않습니다.
        if (activeTarget == null)
        {
            UpdateCurrentTarget();
        }

        UpdateInteractionUI();
    }

    /// <summary>
    /// 주변 상호작용 가능한 객체 중 가장 가까운 객체를
    /// CurrentTarget으로 설정합니다.
    /// </summary>
    private void UpdateCurrentTarget()
    {
        if (nearbyInteractables.Count == 0)
        {
            currentTarget = null;
            return;
        }

        IInteractable closest = null;
        float closestDistance = Mathf.Infinity;

        Vector2 playerPosition = transform.position;

        // foreach (var interactable in nearbyInteractables)
        // {
        //     if (interactable is not MonoBehaviour mono)
        //         continue;

        //     if (mono == null)
        //         continue;

        //     if (!mono.gameObject.activeInHierarchy)
        //         continue;

        //     float distance = Vector2.Distance(
        //         playerPosition,
        //         mono.transform.position
        //     );

        //     if (distance < closestDistance)
        //     {
        //         closestDistance = distance;
        //         closest = interactable;
        //     }
        // }

        // currentTarget = closest;

        for (int i = nearbyInteractables.Count - 1; i >= 0; i--)
        {
            var interactable = nearbyInteractables[i];

            if (interactable is not MonoBehaviour mono || mono == null)
            {
                nearbyInteractables.RemoveAt(i);
                continue;
            }

            if (!mono.gameObject.activeInHierarchy)
            {
                nearbyInteractables.RemoveAt(i);
                continue;
            }

            // 잠겨서 꺼진 상호작용 기능은 범위 목록에 남겨 둡니다.
            // 같은 자리에 서 있는 동안 잠금이 풀려도 바로 다시 찾을 수 있습니다.
            if (!mono.enabled)
                continue;

            float distance = Vector2.Distance(playerPosition, mono.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = interactable;
            }
        }

        currentTarget = closest;
    }

    /// <summary>
    /// 현재 상호작용 대상과의 거리가
    /// 상호작용 가능 거리 이내인지 확인합니다.
    /// </summary>
    private bool IsActiveTargetInRange()
    {
        if (activeTarget is not MonoBehaviour mono)
            return false;

        if (mono == null)
            return false;

        if (!mono.gameObject.activeInHierarchy)
            return false;

        float distance = Vector2.Distance(
            transform.position,
            mono.transform.position
        );

        return distance <= interactionDistance;
    }

    /// <summary>
    /// 현재 상호작용을 시작합니다.
    /// 
    /// PlayerController가 Interact Pressed 입력을 받았을 때 호출합니다.
    /// </summary>
    public void StartInteraction()
    {
        if (activeTarget != null)
            return;

        if (currentTarget == null)
            return;

        // 현재 대상을 고정합니다.
        activeTarget = currentTarget;

        activeTarget.OnInteractPressed();
    }

    /// <summary>
    /// 현재 Hold 상호작용을 진행합니다.
    /// 
    /// ActiveTarget이 IHoldInteractable인 경우에만
    /// OnInteractHeld를 호출합니다.
    /// </summary>
    public void UpdateHoldInteraction()
    {
        if (activeTarget is not IHoldInteractable holdTarget)
            return;

        // 상호작용 대상과 너무 멀어지면 Hold를 취소합니다.
        if (!IsActiveTargetInRange())
        {
            CancelInteraction();
            return;
        }

        holdTarget.OnInteractHeld();
    }

    /// <summary>
    /// 현재 상호작용을 정상적으로 종료합니다.
    /// 
    /// Hold 상호작용이라면 OnInteractReleased를 호출합니다.
    /// </summary>
    public void ReleaseInteraction()
    {
        if (activeTarget is IHoldInteractable holdTarget)
        {
            holdTarget.OnInteractReleased();
        }

        activeTarget = null;
    }

    /// <summary>
    /// 현재 상호작용을 강제로 취소합니다.
    /// 
    /// 다음과 같은 상황에서 사용합니다.
    /// - 상호작용 대상과 너무 멀어짐
    /// - 플레이어가 웅크리기 시작함
    /// - 다른 시스템에서 상호작용을 강제로 취소함
    /// </summary>
    public void CancelInteraction()
    {
        if (activeTarget is IHoldInteractable holdTarget)
        {
            holdTarget.OnInteractReleased();
        }

        activeTarget = null;
    }

    /// <summary>
    /// InteractionPromptUI를 현재 상호작용 대상에 맞게 갱신합니다.
    /// 
    /// 대상 GameObject의 자식 중 "Prompt Point"라는 이름의
    /// Transform을 찾아 UI 위치로 사용합니다.
    /// 해당 오브젝트가 없다면 대상의 Transform을 사용합니다.
    /// </summary>
    private void UpdateInteractionUI()
    {
        if (promptUI == null)
            return;

        if (currentTarget == null)
        {
            promptUI.Hide();
            return;
        }

        if (currentTarget is MonoBehaviour mono)
        {
            Transform promptPoint =
                FindPromptPoint(mono.transform);

            promptUI.SetPosition(
                promptPoint.position
            );
        }

        if (currentTarget is IHoldInteractable holdTarget)
        {
            promptUI.ShowHold(
                holdTarget.HoldProgress
            );
        }
        else
        {
            promptUI.Show();
        }
    }

    /// <summary>
    /// 상호작용 대상의 자식에서 "Prompt Point"라는 이름의
    /// Transform을 검색합니다.
    /// 
    /// 찾지 못한 경우 대상 자신의 Transform을 반환합니다.
    /// </summary>
    private Transform FindPromptPoint(Transform interactionTransform)
    {
        Transform promptPoint =
            interactionTransform.Find(PromptPointName);

        return promptPoint != null
            ? promptPoint
            : interactionTransform;
    }

    /// <summary>
    /// 상호작용 영역에 진입했을 때 호출됩니다.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        IInteractable interactable =
            collision.GetComponentInParent<IInteractable>();

        if (interactable == null)
            return;

        if (!nearbyInteractables.Contains(interactable))
        {
            nearbyInteractables.Add(interactable);
        }
    }

    /// <summary>
    /// 상호작용 영역에서 벗어났을 때 호출됩니다.
    /// </summary>
    private void OnTriggerExit2D(Collider2D collision)
    {
        IInteractable interactable =
            collision.GetComponentInParent<IInteractable>();

        if (interactable == null)
            return;

        nearbyInteractables.Remove(interactable);

        // 현재 Hold 중인 대상이 영역을 벗어나면 취소합니다.
        if (activeTarget == interactable)
        {
            CancelInteraction();
        }

        // 현재 선택된 대상이 영역을 벗어나면 제거합니다.
        if (currentTarget == interactable)
        {
            currentTarget = null;
        }
    }
}
