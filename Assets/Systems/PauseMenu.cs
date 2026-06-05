using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private PlayerInputHandler playerInputHandler;
    [SerializeField] private PlayerModel playerModel;

    private bool isPaused = false;

    void Awake()
    {
        menuRoot.SetActive(false); 
    }

    void Update()
    {
        if (playerInputHandler.EscTriggered)
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