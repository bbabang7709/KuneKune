using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class InventoryUIManager : MonoBehaviour
{
    [Header("Slot Settings")]
    [SerializeField] private int maxConsumableSlots = 10;
    [SerializeField] private int maxKeyItemSlots = 10;
    [SerializeField] private GameObject slotPrefab;

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
    
    private InventorySlotUI[] _consumableSlots;
    private InventorySlotUI[] _keyItemSlots;
    private string _pendingConsumableID;

    void Start()
    {
        confirmationPopup.SetActive(false);

        confirmButton.onClick.AddListener(OnConfirmUseItem);
        cancelButton.onClick.AddListener(() => confirmationPopup.SetActive(false));

        InitializeFixedSlots();
    }

    private void InitializeFixedSlots()
    {
        _consumableSlots = new InventorySlotUI[maxConsumableSlots];
        for (int i = 0; i < maxConsumableSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, consumableContainer);
            _consumableSlots[i] = slotObj.GetComponent<InventorySlotUI>();
            _consumableSlots[i].Clear(this);
        }

        _keyItemSlots = new InventorySlotUI[maxKeyItemSlots];
        for (int i = 0; i < maxKeyItemSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, keyItemContainer);
            _keyItemSlots[i] = slotObj.GetComponent<InventorySlotUI>();
            _keyItemSlots[i].Clear(this);
        }
    }

    public void RefreshUI(List<string> consumables, List<int> counts, List<string> keyItems)
    {
        GameObject firstSlot = null;

        for (int i = 0; i < maxConsumableSlots; i++)
        {
            if (i < consumables.Count)
            {
                _consumableSlots[i].Setup(consumables[i], true, counts[i], this);
                if (firstSlot == null) firstSlot = _consumableSlots[i].gameObject;
            }
            else
            {
                _consumableSlots[i].Clear(this);
            }
        }

        for (int i = 0; i < maxKeyItemSlots; i++)
        {
            if (i < keyItems.Count)
            {
                _keyItemSlots[i].Setup(keyItems[i], false, 1, this);
                if (firstSlot == null) firstSlot = _keyItemSlots[i].gameObject;
            }
            else
            {
                _keyItemSlots[i].Clear(this);
            }
        }

        GameObject targetFocus = firstSlot != null ? firstSlot : _consumableSlots[0].gameObject;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(targetFocus);
    }

    public void UpdateDescriptionPanel(string itemID, bool isConsumable)
    {
        // TODO: ItemDatabase에서 itemID로 이름과 설명을 찾아 텍스트에 적용
        // ItemData data = ItemDatabase.GetItem(itemID);
        // itemNameText.text = data.ItemName;
        // itemDescriptionText.text = data.Description;

        itemNameText.text = itemID;
        itemDescriptionText.text = isConsumable ? "사용 가능한 아이템입니다." : "중요한 물건입니다.";
    }

    public void ClearDescription()
    {
        itemNameText.text = "";
        itemDescriptionText.text = "인벤토리가 비어 있습니다.";
    }

    public void ShowUseConfirmation(string itemID)
    {
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

        confirmationPopup.SetActive(false);
    }
}
