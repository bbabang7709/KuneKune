using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryManager : MonoBehaviour, ISaveable
{
    public static InventoryManager Instance { get; private set; }

    private HashSet<string> _keyItems = new HashSet<string>();
    private Dictionary<string, int> _consumables = new Dictionary<string, int>();

    [Header("UI References")]
    [SerializeField] private GameObject inventoryUIPanel;

    [Tooltip("InventoryUIManager 스크립트를 연결해주세요.")]
    [SerializeField] private InventoryUIManager inventoryUI;

    [Tooltip("PlayerInputHandler 스크립트를 연결해주세요.")]
    [SerializeField] private PlayerInputHandler playerInput;

    public bool IsInventoryOpen => inventoryUIPanel != null && inventoryUIPanel.activeSelf;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        SaveManager.Instance?.Register(this);
        
        inventoryUIPanel?.SetActive(false);
    }

    void OnDestroy()
    {
        SaveManager.Instance?.Unregister(this);
    }

    public string GetUniqueID() => "InventoryManager_Main";

    void Update()
    {
        bool isCutscene = CutsceneManager.Instance != null && CutsceneManager.Instance.IsCutscenePlaying;
        if (playerInput != null && playerInput.IsInventoryPressed && !isCutscene)
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        if (inventoryUIPanel == null) return;
        if (SaveManager.Instance != null && SaveManager.Instance.IsLoading) return;
        if (DialogSystem.Instance != null && DialogSystem.Instance.IsDialogPlaying) return;
        if (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPaused) return;

        bool isOpening = !inventoryUIPanel.activeSelf;

        if (isOpening)
        {
            Time.timeScale = 0f;    // 게임 일시정지
            if (ControlLockManager.Instance != null)
            {
                ControlLockManager.Instance.AddInputLock(this);
                ControlLockManager.Instance.AddMovementLock(this);
            }

            if (inventoryUI != null)
            {
                inventoryUI.RefreshUI(
                    new List<string>(_consumables.Keys),
                    new List<int>(_consumables.Values),
                    new List<string>(_keyItems)
                );
            }
        }
        else
        {
            Time.timeScale = 1f;
            if (ControlLockManager.Instance != null)
            {
                ControlLockManager.Instance.RemoveInputLock(this);
                ControlLockManager.Instance.RemoveMovementLock(this);
            }
        }

        inventoryUIPanel.SetActive(isOpening);
    }

    public void AddItem(ItemData data)
    {
        switch (data.ItemType)
        {
            case EItemType.KeyItem :
                AddKeyItem(data.ItemID);
                break;
            case EItemType.Consumable :
                AddConsumable(data.ItemID, data.ItemCount);
                break;
            default :
                Debug.LogWarning($"[InventoryManager] {data.ItemType}: 존재하지 않는 타입입니다.");
                break;
        }
    }

    public void AddKeyItem(string id) => _keyItems.Add(id);
    public bool HasKey(string id) => _keyItems.Contains(id);

    public void AddConsumable(string id, int count = 1)
    {
        if (_consumables.ContainsKey(id)) _consumables[id] += count;
        else _consumables[id] = count;
    }

    public void SaveState(SaveData data)
    {
        data.keyItemIDs = new List<string>(_keyItems);
        data.consumableItemIDs = new List<string>(_consumables.Keys);
        data.consumableItemCounts = new List<int>(_consumables.Values);
    }

    public void LoadState(SaveData data)
    {
        _keyItems = new HashSet<string>(data.keyItemIDs);
        _consumables.Clear();
        for (int i = 0; i < data.consumableItemIDs.Count; i++)
        {
            _consumables.Add(data.consumableItemIDs[i], data.consumableItemCounts[i]);
        }
    }
}
