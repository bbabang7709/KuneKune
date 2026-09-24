using UnityEngine;

public enum EItemType { None, KeyItem, Consumable }

public abstract class ItemData : ScriptableObject
{
    [SerializeField] private EItemType itemType;
    [SerializeField] private string itemID;
    [SerializeField] private string itemName;

    [Tooltip("인벤토리 UI에서 보여질 아이템 설명입니다.")]
    [TextArea(3, 5)]
    [SerializeField] private string description;

    [SerializeField] [Min(1)] private int itemCount = 1;
    [SerializeField] private Sprite illustration;

    [Header("Consumable Visual")]
    [SerializeField] private Sprite icon;

    [Header("Key Item Visuals (4 States)")]
    [Tooltip("획득 & 미선택")]
    [SerializeField] private Sprite keyAcquiredNormal;

    [Tooltip("획득 & 선택")]
    [SerializeField] private Sprite keyAcquiredSelected;

    [Tooltip("미획득 & 미선택")]
    [SerializeField] private Sprite keyUnacquiredNormal;

    [Tooltip("미획득 & 미선택")]
    [SerializeField] private Sprite keyUnacquiredSelected;

    public EItemType ItemType => itemType;
    public string ItemID => itemID;
    public string ItemName => itemName;
    public string Description => description;
    public int ItemCount => itemCount;
    public Sprite Illustration => illustration;
    public Sprite Icon => icon;
    public Sprite KeyAcquiredNormal => keyAcquiredNormal;
    public Sprite KeyAcquiredSelected => keyAcquiredSelected;
    public Sprite KeyUnacquiredNormal => keyUnacquiredNormal;
    public Sprite KeyUnacquiredSelected => keyUnacquiredSelected;
}
