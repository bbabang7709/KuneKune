using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class SGuide : MonoBehaviour
{
    [SerializeField] private Transform promptPoint;
    [SerializeField] private GameObject guideSprite;

    private InteractionPromptUI _promptUI;
    private PlayerMovement _playerMovement;

    private void Awake()
    {
        BoxCollider2D c = GetComponent<BoxCollider2D>();
        c.isTrigger = true;
        c.excludeLayers = ~LayerMask.GetMask("Player");

        guideSprite.gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        _playerMovement = collision.GetComponentInParent<PlayerMovement>();
        if (_playerMovement == null) return;

        if (_promptUI == null)
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/UI/Interaction Prompt");
            if (prefab == null)
            {
                Debug.LogError("[SGuide] Interaction Prompt 프리팹을 찾을 수 없습니다.", this);
                return;
            }

            // 플레이어의 상호작용 UI와 별도로 표시하여 서로의 안내를 덮어쓰지 않습니다.
            _promptUI = Instantiate(prefab, transform).GetComponent<InteractionPromptUI>();
        }

        if ((ControlLockManager.Instance != null && ControlLockManager.Instance.IsInputLocked) ||
            (SaveManager.Instance != null && SaveManager.Instance.IsLoading))
            return;

        RefreshPrompt();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponentInParent<PlayerMovement>() != _playerMovement) return;
        _playerMovement = null;
        if (_promptUI == null) return;

        _promptUI.Hide();
        guideSprite.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        RefreshPrompt();
    }

    private void RefreshPrompt()
    {
        if (_promptUI == null) return;

        if (_playerMovement == null || _playerMovement.IsCrouching ||
            (ControlLockManager.Instance != null && ControlLockManager.Instance.IsInputLocked) ||
            (SaveManager.Instance != null && SaveManager.Instance.IsLoading))
        {
            _promptUI.Hide();
            guideSprite.SetActive(false);
            return;
        }

        _promptUI.SetPosition(promptPoint != null ? promptPoint.position : transform.position);
        _promptUI.ShowHint("S : 앉기");
        guideSprite.SetActive(true);
    }

    private void OnDisable()
    {
        _playerMovement = null;
        _promptUI?.Hide();
        guideSprite.SetActive(false);
    }
}
