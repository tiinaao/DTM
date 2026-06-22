using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler playerInputHandler;

    private bool isDialogueActive = false;
    private bool isPaused = false;

    public void OnDialogueStart()
    {
        isDialogueActive = true;
        playerInputHandler.DisableInput();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnDialogueEnd()
    {
        isDialogueActive = false;
        if (!isPaused)
        {
            playerInputHandler.EnableInput();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void OnPauseStart()
    {
        isPaused = true;
        playerInputHandler.DisableInput();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnPauseEnd()
    {
        isPaused = false;
        if (!isDialogueActive)
        {
            playerInputHandler.EnableInput();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}