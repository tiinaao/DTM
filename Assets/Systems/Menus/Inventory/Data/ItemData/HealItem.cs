using UnityEngine;

[CreateAssetMenu(fileName = "NewHealItem", menuName = "Inventory/HealItem")]
public class HealItem : ItemData
{
    public float healAmount;

    public override void Use()
    {
        Health h = Health.Instance;
        if (h == null || h.IsFull()) return;
        h.Heal(healAmount);
    }
}