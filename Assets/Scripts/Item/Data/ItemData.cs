using UnityEngine;

public enum EItemType
{
    None,
    KeyItem,
    Consumable
}

public abstract class ItemData : ScriptableObject
{
    [SerializeField] private EItemType itemType;
    [SerializeField] private string itemID;
    [SerializeField] private string itemName;
    [SerializeField] [Min(1)] private int itemCount;
    [SerializeField] private Sprite illustration;
    [SerializeField] private Sprite icon;

    public EItemType ItemType => itemType;
    public string ItemID => itemID;
    public string ItemName => itemName;
    public int ItemCount => itemCount;
    public Sprite Illustration => illustration;
    public Sprite Icon => icon;
}
