using UnityEngine;

public class BusstopEnding : MonoBehaviour, IHoldInteractable
{
    [Header("Hold Interaction Settings")]
    [SerializeField] private float holdDuration = 3f;

    [Header("Ending Cutscene")]
    [SerializeField] private CutsceneControllerBase ending;

    public float HoldDuration => holdDuration;
    public float HoldProgress =>
        holdDuration <= 0f
            ? 1f
            : Mathf.Clamp01(holdTimer / holdDuration);
    
    private float holdTimer;
    private bool hasTriggered = false; 

    public void OnInteractPressed()
    {
        if (hasTriggered) return;
        
        Debug.Log("<color=green>[Busstop] Pressed 호출됨! (타이머 0으로 세팅)</color>");
        holdTimer = 0f;
    }

    public void OnInteractHeld()
    {
        if (hasTriggered) return;

        holdTimer += Time.deltaTime;
        
        // 💡 콘솔창에 타이머가 올라가는지 강제로 확인
        Debug.Log($"<color=orange>[Busstop] Held 호출 중... Timer: {holdTimer:F2}</color>");

        if (holdTimer >= holdDuration)
        {
            hasTriggered = true; 

            if (ending != null)
                ending.PlayCutscene();
            else
                Debug.LogWarning("엔딩 컷씬이 연결되지 않았습니다.");
        }
    }

    public void OnInteractReleased()
    {
        if (hasTriggered) return;
        
        Debug.Log("<color=red>[Busstop] Released 호출됨! (타이머 초기화)</color>");
        
        // 💡 범인 확인을 위해 임시로 타이머 초기화 기능을 꺼둡니다.
        // holdTimer = 0f; 
    }
}