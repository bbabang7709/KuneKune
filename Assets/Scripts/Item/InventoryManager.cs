using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    private readonly List<InventorySlot> slots = new List<InventorySlot>();
    public IReadOnlyList<InventorySlot> Slots => slots;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void AddItem(ItemData item, int count = 1)
    {
        if (item == null || count <= 0)
            return;

        Debug.Log(
            $"[Inventory] 아이템 획득: {item.name} / " +
            $"Type: {item.GetType().Name}"
        );

        if (item is KeyItemData key)
        {
            Debug.Log(
                $"[Inventory] KeyID: [{key.KeyID}]"
            );
        }

        InventorySlot existingSlot = slots.Find(slot => slot.Item == item);

        if (existingSlot != null)
        {
            existingSlot.Count += count;
        }
        else
        {
            slots.Add(new InventorySlot(item, count));
        }

        DebugInventory();
    }

    private void DebugInventory()
    {
        Debug.Log("===== Inventory =====");

        foreach (InventorySlot slot in slots)
        {
            if (slot.Item is KeyItemData key)
            {
                Debug.Log(
                    $"KeyItem : {slot.Item.name} / " +
                    $"KeyID : [{key.KeyID}] / " +
                    $"Count : {slot.Count}"
                );
            }
            else
            {
                Debug.Log(
                    $"Item : {slot.Item.name} / " +
                    $"Type : {slot.Item.GetType().Name} / " +
                    $"Count : {slot.Count}"
                );
            }
        }

        Debug.Log("=====================");
    }

    public bool HasItem(ItemData item)
    {
        return slots.Any(slot => slot.Item == item && slot.Count > 0);
    }

    public bool HasKey(string keyID)
    {
        Debug.Log($"[HasKey] 찾는 KeyID: [{keyID}]");

        foreach (InventorySlot slot in slots)
        {
            if (slot.Item is KeyItemData key)
            {
                Debug.Log(
                    $"[HasKey] 보유 열쇠: [{key.KeyID}], Count: {slot.Count}"
                );

                if (key.KeyID == keyID && slot.Count > 0)
                {
                    Debug.Log("[HasKey] 열쇠 찾음!");
                    return true;
                }
            }
        }

        Debug.Log("[HasKey] 열쇠 못 찾음.");
        return false;
    }

    public bool RemoveItem(ItemData item, int count = 1)
    {
        // 슬롯 찾기
        InventorySlot slot = slots.Find(s => s.Item == item);

        // 슬롯이 없거나 보유한 개수가 적으면 false
        if (slot == null || slot.Count < count) 
            return false;
        
        // 개수 차감
        slot.Count -= count;
        if (slot.Count <= 0)    // 아이템이 0개이면 슬롯 삭제
        {
            slots.Remove(slot);
        }

        return true;
    }

    public bool UseConsumable(ConsumableItemData item)
    {
        // 가지고 있지 않으면 false
        if (!HasItem(item)) return false;

        // TODO: 더위 게이지 차감 로직 구현

        RemoveItem(item);

        return true;
    }
}
