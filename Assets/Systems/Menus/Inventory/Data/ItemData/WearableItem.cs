using UnityEngine;

[CreateAssetMenu(fileName = "NewWearable", menuName = "Inventory/Wearable")]
public class WearableItem : ItemData
{
    [Range(0f, 1f)] public float damageReduction;

    public override void Use()
    {
        
    }
}