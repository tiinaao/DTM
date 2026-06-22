> **Note:** This is an early system and will likely change once a proper map is set up. For now it's mostly for testing and laying the groundwork for the future.

---
## Chunk Loading

The `ChunkLoader` script sits on the **Player** GameObject. It activates and deactivates chunk parent objects based on the player's distance from them, keeping only nearby chunks loaded at any time.

### Adding a New Chunk

1. Create your chunk as a GameObject in the scene and set it up however you need.
2. Select the **Player** in the Hierarchy.
3. In the Inspector, find the `ChunkLoader` component.
4. Expand the **Chunk Parents** list, increase the size by 1, and drag your new chunk GameObject into the new slot.

---

## Warmup System

Unity compiles shaders on demand in the editor, which causes hitching the first time something renders. In exported builds this is handled by the warmup system, which pre-touches shaders, materials, textures, audio, and prefabs during the loading screen so they're ready before the player gets in. There are two components involved: a **global** one that runs once at startup, and a **per-chunk** one that runs as chunks are registered.

---

### Global Warmup (`GlobalWarmupComponent`)

Already set up on the **WarmupManager** GameObject, it handles:

- **TMP text warmup** — already configured, no action needed.
- **Materials** — drag in any materials that are used globally across the whole scene.
- **Textures** — same idea, global textures that aren't tied to a specific chunk.

---

### Per-Chunk Warmup (`ChunkWarmupComponent`)

Each chunk that has unique assets should have a `ChunkWarmupComponent` attached to its root GameObject. 

| Field                  | What to Put Here                                                                                                                   |
| ---------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| `materialsToTouch`     | Materials used by objects in this chunk.                                                                                           |
| `texturesToTouch`      | Textures used in this chunk not already covered by materials.                                                                      |
| `prefabsToInstantiate` | Prefabs that get spawned in this chunk. The warmup briefly instantiates and destroys them to force Unity to compile their shaders. |
| `audioClipsToLoad`     | Audio clips used in this chunk. The warmup calls `LoadAudioData()` on each.                                                        |
| `warmFrames`           | How many frames the warmup is spread across. Higher = smoother but slower.                                                         |

> You can leave any field empty if the chunk doesn't use that type of asset.
