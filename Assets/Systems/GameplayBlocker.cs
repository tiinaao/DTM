using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum BlockFlags
{
    None = 0,
    Movement = 1 << 0,   
    Camera = 1 << 1,   
    Actions = 1 << 2,   
    Interaction = 1 << 3,   
    Inventory = 1 << 4,  
    FreeCursor = 1 << 5,   
    FreezeTime = 1 << 6,   
    All = ~0
}

public static class GameplayBlocker
{
    private static readonly Dictionary<object, BlockFlags> owners = new Dictionary<object, BlockFlags>();

    public static BlockFlags Current { get; private set; } = BlockFlags.None;

    public static event Action<BlockFlags, BlockFlags> Changed;

    public static void Block(object owner, BlockFlags flags)
    {
        if (owner == null || flags == BlockFlags.None) return;

        owners.TryGetValue(owner, out var existing);
        owners[owner] = existing | flags;
        Recalculate();
    }

    public static void Unblock(object owner, BlockFlags flags)
    {
        if (owner == null || !owners.TryGetValue(owner, out var existing)) return;

        var remaining = existing & ~flags;
        if (remaining == BlockFlags.None) owners.Remove(owner);
        else owners[owner] = remaining;
        Recalculate();
    }

    public static void Release(object owner) 
    {
        if (owner == null) return;
        if (owners.Remove(owner)) Recalculate();
    }

    public static bool IsBlocked(BlockFlags flags) => (Current & flags) != 0;

    public static bool IsFullyBlocked(BlockFlags flags) => (Current & flags) == flags;

    public static void ClearAll() 
    {
        owners.Clear();
        Recalculate();
    }

    private static void Recalculate()
    {
        var old = Current;
        var combined = BlockFlags.None;
        foreach (var kv in owners) combined |= kv.Value;

        if (combined == old) return;
        Current = combined;

        ApplyBuiltIns(old, combined);
        Changed?.Invoke(old, combined);
    }

    private static void ApplyBuiltIns(BlockFlags old, BlockFlags now)
    {
        bool cursorWas = (old & BlockFlags.FreeCursor) != 0;
        bool cursorNow = (now & BlockFlags.FreeCursor) != 0;
        if (cursorWas != cursorNow)
        {
            Cursor.visible = cursorNow;
            Cursor.lockState = cursorNow ? CursorLockMode.None : CursorLockMode.Locked;
        }

        bool timeWas = (old & BlockFlags.FreezeTime) != 0;
        bool timeNow = (now & BlockFlags.FreezeTime) != 0;
        if (timeWas != timeNow)
        {
            Time.timeScale = timeNow ? 0f : 1f;
        }
    }
}