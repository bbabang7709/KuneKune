using UnityEngine;

[CreateAssetMenu(fileName = "Key Item", menuName = "KuneKune/Item/Key")]
public class KeyItemData : ItemData
{
    [SerializeField] private string keyID;
    public string KeyID => keyID;
}
