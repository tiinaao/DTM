using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private PlayerInputHandler playerInputHandler;
    [SerializeField] private InventoryUI inventoryUI;

    private const BlockFlags PauseBlocks =
        BlockFlags.Movement | BlockFlags.Camera | BlockFlags.Actions |
        BlockFlags.Interaction | BlockFlags.Inventory |
        BlockFlags.FreeCursor | BlockFlags.FreezeTime;

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

        if (isPaused) GameplayBlocker.Block(this, PauseBlocks);
        else GameplayBlocker.Release(this);
    }

    void OnDisable()
    {
        GameplayBlocker.Release(this); 
    }
}