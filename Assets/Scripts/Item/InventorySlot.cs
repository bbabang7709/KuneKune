
[System.Serializable]
public class InventorySlot
{
    public ItemData Item;
    public int Count;

    public InventorySlot(ItemData item, int count)
    {
        Item = item;
        Count = count;
    }
}
