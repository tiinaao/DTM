using UnityEngine;

public enum ItemCategory
{
    Consumable,
    Weapon,
    Wearable,
    Other
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemId => itemName.ToLower().Replace(" ", "_");

    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public ItemCategory category;

    public bool isStackable = true;
    public int maxStack = 99;

    public bool isEquippable;

    public virtual void Use() { }
}