using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Unity.VisualScripting;

public class InventorySlotUI : MonoBehaviour, ISelectHandler, IDeselectHandler, ISubmitHandler, IPointerClickHandler
{
    [Header("Slot Background (Consumables Only)")]
    [SerializeField] private Image slotBackgroundImage;
    [SerializeField] private Sprite normalSlotSprite;
    [SerializeField] private Sprite selectedSlotSprite;

    [Header("Item Graphics")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;

    private string _itemID;
    private bool _isConsumable;
    private bool _isEmpty = true;
    private bool _isAcquired = true;
    private bool _isSelected = false;
    private bool _isVisualLocked = false;   // Confirmation 팝업이 떠 있는 동안에 유지될 플래그

    private InventoryUIManager _uiManager;

    public void SetupConsumable(string itemID, int count, InventoryUIManager uiManager)
    {
        _itemID = itemID;
        _isConsumable = true;
        _uiManager = uiManager;
        _isEmpty = false;
        _isAcquired = true;

        ItemData data = _uiManager.GetItemData(itemID);
        if (data != null && data.Icon != null)
        {
            iconImage.sprite = data.Icon;
            iconImage.color = Color.white;
        }
        iconImage.gameObject.SetActive(true);

        countText.text = count > 1 ? count.ToString() : "";
        countText.gameObject.SetActive(count > 1);

        if (slotBackgroundImage != null) slotBackgroundImage.enabled = true;

        UpdateVisuals();
    }

    public void SetupKeyItem(string itemID, bool isAcquired, InventoryUIManager uiManager)
    {
        _itemID = itemID;
        _isConsumable = false;
        _uiManager = uiManager;

        _isEmpty = !isAcquired;
        _isAcquired = isAcquired;

        iconImage.gameObject.SetActive(true);
        countText.gameObject.SetActive(false);

        if (slotBackgroundImage != null) slotBackgroundImage.enabled = false;

        UpdateVisuals();
    }

    // public void Setup(string itemID, bool isConsumable, int count, InventoryUIManager uiManager, Sprite icon = null)
    // {
    //     _itemID = itemID;
    //     _isConsumable = isConsumable;
    //     _uiManager = uiManager;
    //     _isEmpty = false;

    //     ItemData data = _uiManager.GetItemData(itemID);
    //     if (data != null && data.Icon != null)
    //     {
    //         iconImage.sprite = data.Icon;
    //     }
    //     iconImage.gameObject.SetActive(true);

    //     countText.text = count > 1 ? count.ToString() : "";
    //     countText.gameObject.SetActive(count > 1);
    // }

    public void Clear(InventoryUIManager uIManager, bool isConsumable = false)
    {
        _itemID = string.Empty;
        _isEmpty = true;
        _isSelected = false;
        _isConsumable = isConsumable;
        _uiManager = uIManager;

        iconImage.gameObject.SetActive(false);
        countText.gameObject.SetActive(false);

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        bool activeHighlight = _isSelected || _isVisualLocked;

        if (_isConsumable && slotBackgroundImage != null)
        {
            slotBackgroundImage.sprite = activeHighlight ? selectedSlotSprite : normalSlotSprite;
        }

        if (string.IsNullOrEmpty(_itemID)) return;

        ItemData data = _uiManager.GetItemData(_itemID);
        if (data == null) return;

        if (!_isConsumable)
        {
            if (_isAcquired)
            {
                iconImage.sprite = _isSelected ? data.KeyAcquiredSelected : data.KeyAcquiredNormal;
            }
            else
            {
                iconImage.sprite = _isSelected ? data.KeyUnacquiredSelected : data.KeyUnacquiredNormal;
            }

            iconImage.SetNativeSize();
        }
    }

    public void ForceUpdateDescription()
    {
        if (_uiManager == null) return;

        if (_isConsumable)
        {
            if (_isEmpty && _isConsumable) _uiManager.ClearDescription();
            else _uiManager.UpdateDescriptionPanel(_itemID);
        }
        else
        {
            if (!_isAcquired) _uiManager.UpdateUnknownDescription();
            else _uiManager.UpdateDescriptionPanel(_itemID);
        }
        
    }

    public void OnSelect(BaseEventData eventData)
    {
        _isSelected = true;
        UpdateVisuals();

        _uiManager.OnSlotSelected(this, _itemID, _isConsumable, _isEmpty, _isAcquired);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _isSelected = false;
        // _uiManager.ClearDescription();
        UpdateVisuals();
    }

    public void ForceDeselect()
    {
        _isSelected = false;
        // _uiManager.ClearDescription();
        _isVisualLocked = false;
        UpdateVisuals();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (_isEmpty || !_isConsumable) return;
        _uiManager.ShowUseConfirmation(_itemID);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isEmpty) return;
        EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void SetVisualLock(bool isLocked)
    {
        _isVisualLocked = isLocked;
        UpdateVisuals();
    }
}
