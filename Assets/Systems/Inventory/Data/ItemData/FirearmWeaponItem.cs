using UnityEngine;

[CreateAssetMenu(fileName = "NewFirearmWeapon", menuName = "Inventory/FirearmWeapon")]
public class FirearmWeaponItem : ItemData
{
    public float damage;
    public int magazineCapacity;

    public override void Use()
    { 

    }
}