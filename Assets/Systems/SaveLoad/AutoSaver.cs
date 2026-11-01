using UnityEngine;

public class AutoSaver : MonoBehaviour
{
    [SerializeField] private float intervalSeconds = 600f;

    private float timer;

    private void Update()
    {
        timer += Time.unscaledDeltaTime;
        if (timer < intervalSeconds) return;

        timer = 0f;
        SaveSystem.AutoSave();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveSystem.AutoSave();
    }

    private void OnApplicationQuit()
    {
        SaveSystem.AutoSave();
    }
}