using UnityEngine;
using UnityEngine.InputSystem;

public class SaveTest : MonoBehaviour
{
    [SerializeField] private int testSlot = 1;

    void Update()
    {
        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            bool ok = SaveSystem.Save(testSlot);
            Debug.Log(ok ? "Saved to slot " + testSlot : "Save failed");
        }

        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            LoadResult result = SaveSystem.Load(testSlot);
            Debug.Log("Load result: " + result);
        }

        if (Keyboard.current.f8Key.wasPressedThisFrame)
        {
            bool ok = SaveSystem.AutoSave();
            Debug.Log(ok ? "Recovery file written" : "Auto-save skipped (no active slot)");
        }

        if (Keyboard.current.f10Key.wasPressedThisFrame)
        {
            LoadResult result = SaveSystem.LoadRecovery();
            Debug.Log("Recovery load result: " + result);
        }
    }
}