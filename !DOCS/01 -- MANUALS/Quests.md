<hr>
## How It Works

Every quest is a `QuestInfoSO` asset. At startup the `QuestManager` loads all of them, wraps each in a `Quest`, and tracks its state. Quests are made of steps. Each step is a prefab with a script that inherits from `QuestStep`. Only one step is alive at a time. When it finishes, the next one spawns.

| State | Meaning |
| --- | --- |
| `REQUIREMENTS_NOT_MET` | Prerequisite quests are not finished yet. |
| `CAN_START` | Prerequisites are done, the player may start the quest. |
| `IN_PROGRESS` | A step is currently active. |
| `CAN_FINISH` | All steps are done, waiting to be turned in. |
| `FINISHED` | Rewards given, quest complete. |

---
## Creating a New Quest

1. Go to `Assets/Resources/Quests/`. **The asset must live in a `Resources/Quests` folder**, otherwise the QuestManager will not find it.
2. **Right-click → Create → Quests → QuestInfoSO**.
3. Fill in the fields below.

| Field                | Notes                                                                                                           |
| -------------------- | --------------------------------------------------------------------------------------------------------------- |
| `displayName`        | Name of the quest. Also used to generate the quest id.                                                          |
| `questPrerequisites` | Quests that must be `FINISHED` before this one can start. Leave empty for a quest available from the beginning. |
| `questStepPrefabs`   | Ordered list of step prefabs. Element 0 runs first.                                                             |
| `moraReward`         | Mora given on finish.                                                                                           |
| `reputationReward`   | Reputation change on finish. Can be negative.                                                                   |
| `itemRewards`        | List of `item` (ItemData) and `amount` pairs.                                                                   |

> Quest id is generated automatically from `displayName` (lowercase, spaces become underscores, so "Find The Key" becomes `find_the_key`).

> Keep `displayName` unique and do not rename it after release. Two quests with the same name overwrite each other, and renaming a quest changes its id so existing save data will no longer match it.

---
## Creating a Quest Step

A step is a prefab with a script that extends `QuestStep`. It has three things to know:

| Member                            | Purpose                                                                      |
| --------------------------------- | ---------------------------------------------------------------------------- |
| `SetQuestStepState(string state)` | Called when the step is restored from a save.                                |
| `ChangeState(string newState)`    | Call whenever progress changes so it gets saved.                             |
| `FinishQuestStep()`               | Call when the step is done. Advances the quest and destroys the step object. |

Example, a step that counts things:

```csharp
public class KillEnemiesStep : QuestStep
{
    [SerializeField] private int required = 5;
    private int killed;

    public void OnEnemyKilled()
    {
        killed++;
        ChangeState(killed.ToString());   // saves progress
        if (killed >= required) FinishQuestStep();
    }

    protected override void SetQuestStepState(string state)
    {
        int.TryParse(state, out killed);  // restores progress from save
    }
}
```

Then:

1. Create an empty GameObject, add your step script, and turn it into a prefab (place it in the `Prefab/` folder).
2. Add the prefab to the quest's `questStepPrefabs` list, in the order the steps should happen.

> Every element of `questStepPrefabs` must be a valid prefab with a `QuestStep` component. An empty slot will leave the quest stuck, and a quest with zero steps starts but can never reach `CAN_FINISH`.

---
## Placing a Quest Point (Start / Turn-in)

A `QuestPoint` is what the player interacts with to start or finish a quest, usually on an NPC.

1. Add the `QuestPoint` script to the object (NPC, sign, noticeboard).
2. Add a **Collider** set to **Is Trigger** so the player can walk into range. The player must have the `Player` tag.
3. Fill in the fields:

| Field | Notes |
| --- | --- |
| `questInfoForPoint` | The quest this point belongs to. |
| `startPoint` | Player can start the quest here (default on). |
| `finishPoint` | Player can turn in the quest here (default on). |

4. Optional: add a child object with the `QuestIcon` script to show the state above the point.

The prompt shows **Start Quest** when the quest is `CAN_START`, and **Finish Quest** when it is `CAN_FINISH`. At any other time there is no prompt. Interaction goes through the `InteractionSelector` and the interact key, and is blocked while gameplay interaction is blocked (menus, dialogue, etc.).

> Start and finish can be different objects. Put one point with only `startPoint` on the quest giver and another with only `finishPoint` on the person who gets the turn-in.

### Setting Up the Quest Icon

`QuestIcon` needs four child objects assigned, one for each icon:

| Field | Shown when |
| --- | --- |
| `requirementsNotMetToStartIcon` | Quest is locked and this point is a start point. |
| `canStartIcon` | Quest can be started and this point is a start point. |
| `requirementsNotMetToFinishIcon` | Quest is in progress and this point is a finish point. |
| `canFinishIcon` | Quest can be finished and this point is a finish point. |

All icons are hidden when the quest is `FINISHED`. All four fields must be filled or the icon will throw errors.

---
## Starting Quests Automatically (Quest Trigger)

Use `QuestTrigger` when a quest should start by itself instead of through a Quest Point. It only fires while the quest is `CAN_START`, and checks its condition every `checkInterval` seconds (default 0.25).

| Type | Starts the quest when |
| --- | --- |
| `EnterArea` | The player enters the trigger collider. Needs a trigger Collider on the object. |
| `HasItem` | Inventory holds `requiredAmount` of `requiredItem`. |
| `TimeState` | The time of day equals `requiredTimeState`. |
| `MinReputation` | Reputation is at least `minReputation`. |
| `Automatic` | Immediately, as soon as the quest is `CAN_START`. |
| `Manual` | Never on its own. Call `TriggerStart()` from code. |

Set the `quest` field to the quest to start, then fill only the fields that match the chosen type.

---
## Rewards

When the player finishes a quest, the QuestManager gives:

- `moraReward` to the player's currency.
- Every entry in `itemRewards` to the inventory.
- `reputationReward` to reputation.

> Reward items must exist in the `ItemDatabase`, since they are added by item id. If the inventory is full, the item is not added and a warning is logged. Entries with no item or an amount of 0 or less are skipped.

---
## Prerequisites and Quest Chains

A quest becomes `CAN_START` only once every quest in its `questPrerequisites` is `FINISHED`. To make a chain, put quest A in the prerequisites of quest B, and B in C. Quests with no prerequisites unlock on scene start. Empty slots in the list are ignored.

---
## Saving and Loading

The QuestManager exposes `Save(ref QuestSaveData data)` and `Load(QuestSaveData data)`. Each quest stores its state, its current step index and the saved string of each step. On load, the quest rebuilds itself, respawns the current step and hands it the saved string through `SetQuestStepState`. This is why every step should call `ChangeState` when its progress changes.

---
## Using the Quest System From Code

All communication goes through `GameEventsManager.instance.questEvents`.

| Event | Use |
| --- | --- |
| `StartQuest(id)` | Start a quest that is `CAN_START`. |
| `FinishQuest(id)` | Finish a quest that is `CAN_FINISH` and give rewards. |
| `AdvanceQuest(id)` | Used by steps internally. Do not call it yourself, use `FinishQuestStep()`. |
| `onQuestStateChange` | Subscribe to react to any quest changing, for example to update a UI. |

To check a quest's state:

```csharp
if (GameManager.Instance.Quests.TryGetState("quest_id", out QuestState state)
    && state == QuestState.FINISHED)
```

When subscribing to an event, always unsubscribe in `OnDisable`, and check that `GameEventsManager.instance` is not null first.