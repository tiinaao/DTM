<hr>
## How It Works

Every saveable component registers itself onto a property on `GameManager` when it wakes up. Then, `SaveSystem`  asks `GameManager.Instance` for whichever component it needs, and calls that component's own `Save`/`Load` methods directly.

**So the chain is: `SaveSystem` → `GameManager.Instance.X` → `X.Save()` / `X.Load()`**.

**Save File path:** `C:\Users\<user>\AppData\LocalLow\<company name>\DTM\save.save`

<hr>
## Adding to Save/Load

To make a new object saveable:

- Add `Save` and `Load` functions to the script of the object you want to save. Reference other scripts that already have this implemented (e.g. `PlayerModel`, `Health`, `InventorySystem`).
- Add a persistent instance property for it on `GameManager`, and register the object onto it (usually in `Awake()`).
- Add the corresponding save/load handling to `SaveSystem` — a field in the `SaveData` struct, plus a call in `HandleSaveData()` and `HandleLoadData()`.

---
## Triggering Save / Load

Both are static, so no reference setup is needed:

```csharp
SaveSystem.Save();
SaveSystem.Load();
```

---
## Known Editor Behaviour

Console warning on stopping Play mode:

```
Some objects were not cleaned up when closing the scene. (Did you spawn new GameObjects from OnDestroy?)
```

This is expected and not a bug. `GameManager` is marked as `DontDestroyOnLoad`, so it lives in a special persistent scene that survives normal scene unloads. 

---

> _Important:_ If a component using a `CharacterController` doesn't visually move after `Load()` sets its `transform.position`, disable the `CharacterController` before setting the position and re-enable it afterward. The controller can silently override direct transform changes otherwise.