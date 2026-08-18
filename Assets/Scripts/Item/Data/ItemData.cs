using UnityEngine;

public abstract class ItemData : ScriptableObject
{
    [SerializeField] private string itemID;
    [SerializeField] private string itemName;
    [SerializeField] private Sprite icon;

    public string ItemID => itemID;
    public string ItemName => itemName;
    public Sprite Icon => icon;
}
