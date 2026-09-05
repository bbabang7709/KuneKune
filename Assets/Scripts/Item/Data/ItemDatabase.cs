using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item Database", menuName = "KuneKune/Item/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    [Tooltip("게임 내 모든 아이템(ItemData)을 여기에 등록하세요.")]
    public List<ItemData> allItems = new List<ItemData>();

    private Dictionary<string, ItemData> _itemDict;

    public ItemData GetItem(string itemID)
    {
        if (_itemDict == null || _itemDict.Count == 0)
        {
            _itemDict = new Dictionary<string, ItemData>();
            foreach (var item in allItems)
            {
                if (item != null && !string.IsNullOrEmpty(item.ItemID))
                {
                    _itemDict[item.ItemID] = item;
                }
            }
        }

        if (_itemDict.TryGetValue(itemID, out ItemData data))
        {
            return data;
        }

        Debug.LogWarning($"[ItemDatabase] '{itemID}' 아이템을 찾을 수 없습니다. DB 등록을 확인하세요.");
        return null;
    }
}
