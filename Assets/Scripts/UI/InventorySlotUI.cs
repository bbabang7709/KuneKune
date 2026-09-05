using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlotUI : MonoBehaviour, ISelectHandler, ISubmitHandler, IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;

    private string _itemID;
    private bool _isConsumable;
    private bool _isEmpty = true;
    private InventoryUIManager _uiManager;

    public void Setup(string itemID, bool isConsumable, int count, InventoryUIManager uiManager, Sprite icon = null)
    {
        _itemID = itemID;
        _isConsumable = isConsumable;
        _uiManager = uiManager;
        _isEmpty = false;

        iconImage.sprite = icon;
        iconImage.gameObject.SetActive(true);

        countText.text = count > 1 ? count.ToString() : "";
        countText.gameObject.SetActive(count > 1);
    }

    public void Clear(InventoryUIManager uIManager)
    {
        _itemID = string.Empty;
        _isEmpty = true;
        _uiManager = uIManager;

        iconImage.gameObject.SetActive(false);
        countText.gameObject.SetActive(false);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (_uiManager == null) return;

        if (_isEmpty)
        {
            _uiManager.ClearDescription();
        }
        else
        {
            _uiManager.UpdateDescriptionPanel(_itemID, _isConsumable);   
        }
    }

    public void OnSubmit(BaseEventData eventData)
    {
        ExecuteSlotAction();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ExecuteSlotAction();
    }

    private void ExecuteSlotAction()
    {
        if (_isEmpty) 
        {
            Debug.Log("비어있는 슬롯입니다.");
            return;
        }

        if (_isConsumable)
        {
            _uiManager.ShowUseConfirmation(_itemID);
        }
        else
        {
            Debug.Log($"[Inventory] {_itemID} (중요 아이템)은 여기서 사용할 수 없습니다.");
        }
    }
}
