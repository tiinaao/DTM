using UnityEngine;
using UnityEngine.InputSystem;

public class SaveTest : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            SaveSystem.Save();
            Debug.Log("Saved.");
        }

        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            SaveSystem.Load();
            Debug.Log("Loaded.");
        }
    }
}