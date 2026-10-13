using UnityEngine;

public class EndingSetupTrigger : MonoBehaviour
{
    [Header("Ending Settings")]
    [Tooltip("평상시에 상호작용하던 기본 버스 정류장 오브젝트")]
    [SerializeField] private GameObject normalInteractObject; 
    
    [Tooltip("진엔딩 조건을 맞췄을 때 켜질 홀드용 버스 정류장 오브젝트")]
    [SerializeField] private GameObject endingInteractObject; 

    [Tooltip("진엔딩 조건을 달성한 상태로 도착하면 재생할 컷씬")]
    [SerializeField] private CutsceneControllerBase trueEndingCutscene;

    private void Setup(Collider2D playerCollider)
    {
        if (MapManager.Instance != null)
            MapManager.Instance.KuneKune?.Despawn();

        if (SaveManager.Instance != null && SaveManager.Instance.IsTrueEndingUnlocked &&
            trueEndingCutscene != null)
        {
            trueEndingCutscene.PlayCutscene();
            return;
        }
            
        // 💡 1. 기존 상호작용 오브젝트는 아예 꺼버립니다. (콜라이더도 함께 꺼짐)
        if (normalInteractObject != null) 
            normalInteractObject.SetActive(false);
            
        // 💡 2. 새로운 엔딩 전용 오브젝트를 켭니다. (플레이어 시스템이 새롭게 인식함)
        if (endingInteractObject != null) 
            endingInteractObject.SetActive(true);

        PlayerAppearance playerApp = playerCollider.gameObject.GetComponent<PlayerAppearance>();
        if (playerApp != null)
            playerApp.RefreshCollider();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isActiveAndEnabled && collision.CompareTag("Player"))
        {
            // 중복 실행 방지
            this.enabled = false; 
            Setup(collision);
        }
    }
}
