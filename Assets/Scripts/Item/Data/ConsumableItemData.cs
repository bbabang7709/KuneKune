using UnityEngine;

[CreateAssetMenu(fileName = "Consumable Item", menuName = "KuneKune/Item/Consumable")]
public class ConsumableItemData : ItemData
{
    [SerializeField] private float heatRecovery;
    public float HeatRecovery => heatRecovery;
}
