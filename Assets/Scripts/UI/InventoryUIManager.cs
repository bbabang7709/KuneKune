using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class InventoryUIManager : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("Slot Settings")]
    [Tooltip("소비 아이템 최대 슬롯 개수(2 x 8)")]
    [SerializeField] private int maxConsumableSlots = 16;
    [Tooltip("도감처럼 보여줄 키 아이템들의 ID를 순서대로 입력하세요.")]
    [SerializeField] private List<string> fixedKeyItemIDs;

    [Header("Slot Prefabs")]
    [Tooltip("소비 아이템을 담을 Slot 프리팹")]
    [SerializeField] private GameObject consumableSlotPrefab;
    [Tooltip("Key 아이템을 담을 프리팹")]
    [SerializeField] private GameObject keyItemSlotPrefab;

    [Header("Layout Containers")]
    [SerializeField] private Transform consumableContainer;
    [SerializeField] private Transform keyItemContainer;

    [Header("Description Panel")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;

    [Header("Confirmation Popup")]
    [SerializeField] private GameObject confirmationPopup;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("BLock Settings")]
    [Tooltip("Confirm 팝업이 뜰 때 입력을 차단할 Panel들의 CanvasGroup을 모두 넣어주세요")]
    [SerializeField] private CanvasGroup[] backgroundGroupsToLock;

    [Header("Use Button")]
    [SerializeField] private Button useItemButton;
    
    private InventorySlotUI[] _consumableSlots;
    private InventorySlotUI[] _keyItemSlots;
    private string _pendingConsumableID;
    private string _currentlySelectedItemID;

    // private GameObject _lastSelectedSlot;

    private InventorySlotUI _currentActiveSlot;

    void Start()
    {
        confirmationPopup.SetActive(false);
        confirmButton.onClick.AddListener(OnConfirmUseItem);
        cancelButton.onClick.AddListener(CloseConfirmationPopup);

        if (useItemButton != null) useItemButton.onClick.AddListener(OnClickUseButton);

        InitializeFixedSlots();
    }

    void Update()
    {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && !confirmationPopup.activeSelf)
        {
            if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
            {
                EventSystem.current.SetSelectedGameObject(_consumableSlots[0].gameObject);
            }
        }
    }

    private void InitializeFixedSlots()
    {
        _consumableSlots = new InventorySlotUI[maxConsumableSlots];
        for (int i = 0; i < maxConsumableSlots; i++)
        {
            GameObject slotObj = Instantiate(consumableSlotPrefab, consumableContainer);
            _consumableSlots[i] = slotObj.GetComponent<InventorySlotUI>();
            _consumableSlots[i].Clear(this, true);
        }

        _keyItemSlots = new InventorySlotUI[fixedKeyItemIDs.Count];
        for (int i = 0; i < fixedKeyItemIDs.Count; i++)
        {
            GameObject slotObj = Instantiate(keyItemSlotPrefab, keyItemContainer);
            _keyItemSlots[i] = slotObj.GetComponent<InventorySlotUI>();
            _keyItemSlots[i].Clear(this, false);
        }
    }

    public void RefreshUI(List<string> consumables, List<int> counts, HashSet<string> acquiredKeyItems)
    {
        confirmationPopup.SetActive(false);
        _pendingConsumableID = string.Empty;
        _currentlySelectedItemID = string.Empty;

        if (_consumableSlots != null)
        {
            foreach (var slot in _consumableSlots) slot?.ForceDeselect();
        }
        if (_keyItemSlots != null)
        {
            foreach (var slot in _keyItemSlots) slot?.ForceDeselect();
        }

        for (int i = 0; i < maxConsumableSlots; i++)
        {
            if (i < consumables.Count)
            {
                _consumableSlots[i].SetupConsumable(consumables[i], counts[i], this);
            }
            else
            {
                _consumableSlots[i].Clear(this, true);
            }
        }

        for (int i = 0; i < fixedKeyItemIDs.Count; i++)
        {
            string keyID = fixedKeyItemIDs[i];
            bool isAcquired = acquiredKeyItems.Contains(keyID);

            _keyItemSlots[i].SetupKeyItem(keyID, isAcquired, this);
        }
        EventSystem.current.SetSelectedGameObject(null);
        ClearDescription();
        if (useItemButton != null) useItemButton.gameObject.SetActive(false);
    }

    public void OnSlotSelected(InventorySlotUI slot, string itemID, bool isConsumable, bool isEmpty, bool isAcquired)
    {
        _currentActiveSlot = slot;
        _currentlySelectedItemID = itemID;

        if (isConsumable)
        {
            if (isEmpty)
            {
                ClearDescription();
                if (useItemButton != null) useItemButton.gameObject.SetActive(false);
                return;
            }
            else
            {
                UpdateDescriptionPanel(itemID);
                if (useItemButton != null) useItemButton.gameObject.SetActive(true);   
            }
        }
        else
        {
            if (!isAcquired) UpdateUnknownDescription();
            else UpdateDescriptionPanel(itemID);

            if (useItemButton != null) useItemButton.gameObject.SetActive(false);
        }
    }

    private void OnClickUseButton()
    {
        if (!string.IsNullOrEmpty(_currentlySelectedItemID))
        {
            ShowUseConfirmation(_currentlySelectedItemID);
        }
    }

    public void UpdateDescriptionPanel(string itemID)
    {
        ItemData data = GetItemData(itemID);

        if (data != null)
        {
            itemNameText.text = data.ItemName;
            itemDescriptionText.text = data.Description;
        }
        else
        {
            itemNameText.text = "Unknown";
            itemDescriptionText.text = "데이터를 불러올 수 없습니다.";
        }
    }

    public void UpdateUnknownDescription()
    {
        itemNameText.text = "???";
        itemDescriptionText.text = "???";
    }

    public void ClearDescription()
    {
        itemNameText.text = "";
        itemDescriptionText.text = "";
    }

    public void ShowUseConfirmation(string itemID)
    {
        if (_currentActiveSlot != null) _currentActiveSlot.SetVisualLock(true);
        SetBackgroundInteractable(false);

        _pendingConsumableID = itemID;
        confirmationPopup.SetActive(true);

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(cancelButton.gameObject);
    }

    private void OnConfirmUseItem()
    {
        if (!string.IsNullOrEmpty(_pendingConsumableID))
        {
            Debug.Log($"[Inventory] {_pendingConsumableID} 사용");
            // TODO: 인벤토리에서 사용 로직 호출
            // TODO: 플레이어 더위 회복 로직 호출
        }

        CloseConfirmationPopup();
    }

    private void CloseConfirmationPopup()
    {
        confirmationPopup.SetActive(false);

        SetBackgroundInteractable(true);

        if (_currentActiveSlot != null && _currentActiveSlot.gameObject.activeInHierarchy)
        {
            _currentActiveSlot.SetVisualLock(false);

            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(_currentActiveSlot.gameObject);
        }
        else
        {
            EventSystem.current.SetSelectedGameObject(_consumableSlots[0].gameObject);
        }
    }

    private void SetBackgroundInteractable(bool isInteractable)
    {
        if (backgroundGroupsToLock == null) return;
        foreach (CanvasGroup group in backgroundGroupsToLock)
        {
            if (group != null) group.interactable = isInteractable;
        }
    }

    public ItemData GetItemData(string itemID)
    {
        return itemDatabase != null ? itemDatabase.GetItem(itemID) : null;
    }
}
