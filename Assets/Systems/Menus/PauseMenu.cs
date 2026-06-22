using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private PlayerInputHandler playerInputHandler;
    [SerializeField] private PlayerModel playerModel;
    [SerializeField] private InventoryUI inventoryUI;

    private bool isPaused = false;

    void Awake()
    {
        menuRoot.SetActive(false);
    }

    void Update()
    {
        if (!playerInputHandler.EscTriggered) return;

        if (inventoryUI != null && inventoryUI.IsOpen)
        {
            inventoryUI.ToggleInventory();
            return;
        }

        TogglePause();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        menuRoot.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
        Cursor.visible = isPaused;
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        playerModel.enabled = !isPaused;
    }
}