<hr>

**Inventory folder path:** `Assets/Systems/Menus/Inventory/`  

---
## Creating a New Item

Navigate to `Assets/Systems/Menus/Inventory/Items/` and go into the corresponding subfolder (e.g. `Consumables`, `Weapons`) or stay in `Items/` itself. **Right-click → Create → Inventory** and pick the item type that fits:

| Type            | When to Use                                                                                                     |
| --------------- | --------------------------------------------------------------------------------------------------------------- |
| `Item`          | Items with no specific behaviour. Basically an empty shell, good for anything that doesn't need a use function. |
| `HealItem`      | Consumables that restore health. The heal function is already implemented, just set the `healAmount`.           |
| `FirearmWeapon` | Guns. Has `damage` and `magazineCapacity` fields.                                                               |
| `MeleeWeapon`   | Melee weapons. Has a `damage` field.                                                                            |
| `Wearable`      | Armour or clothing. Has a `damageReduction` field.                                                              |
> You can also create your own item type by extending `ItemData`, make sure your class inherits from it so it pulls in all the required fields automatically. Reference the existing item scripts for how to do it, they can be found in `Inventory/Data/ItemData/`.

> Do not create another `ItemDatabase`. There is one database that holds every item in the game, adding a second one will break lookups. 

---
## Filling In the Item Data

Once the asset is created, fill in these fields in the Inspector:

| Field          | Notes                                                               |
| -------------- | ------------------------------------------------------------------- |
| `itemName`     | The display name shown in the inventory UI.                         |
| `description`  | A short description of the item.                                    |
| `icon`         | Drag your sprite here. See below for how to import icons correctly. |
| `category`     | Set to match the item type: Consumable, Weapon, Wearable, or Other. |
| `isStackable`  | Whether multiple of this item stack in one slot.                    |
| `maxStack`     | Maximum stack size (default 99).                                    |
| `isEquippable` | Whether the item can be equipped.                                   |
### Importing an Icon

1. Drop the file into `Assets/Systems/Menus/Inventory/Items/Sprites/`.
2. In the Inspector, set **Texture Type** to **Sprite (2D and UI)**.
3. Set **Sprite Mode** to **Single**.
4. Click **Apply**.
5. Drag the sprite into the `Icon` field on your `ItemData` asset.

---
## Adding to the Database

The `ItemDatabase` ScriptableObject holds every single item that can exist in the game. It's what the inventory system queries when adding, removing, or checking for items, so if an item isn't in the database, the game won't know it exists.

To add your new item:

1. Find the `ItemDatabase` asset in `Assets/Systems/Menus/Inventory/Data`.
2. Select it and in the Inspector, expand the **All Items** list.
3. Increase the size by 1 and drag your new `ItemData` asset into the new slot.

---
## Equipment Slots

Players can have the following equipped at once:

- **2 weapons** — primary and secondary, switched using the **scroll wheel**.
- **2 consumables** — mapped to **G** and **R**.

Equipping a third weapon or consumable will replace one of the slots.

---
## Adding Items via Code

1. Find the `ItemPickup` script in `Inventory/` and attach it to the object that should give out items.
2. In the Inspector, expand the **Items** list, set the size, then drag in each `ItemData` asset and set the amount per entry.
3. Call `GiveItem()` whenever you want the items to be handed to the player. For an example of how to call it, reference the `InteractPrompt` script found in `Player/Camera/`.

## *Debug:* Manually Adding Items to the Inventory

1. In the Hierarchy, find the GameObject **Menu → Inventory**.
2. Select the `InventorySystem` component on that object.
3. In the Inspector you'll see lists for `consumables`, `weapons`, `wearables`, and `others`.
4. Add your item into the corresponding list directly.

> *Important:* Always set item amount to > 0. Items added with amount 0 will appear in the inventory but be immediately unequipped from the quickslot the first time any consumable use button is pressed.