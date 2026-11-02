using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(ShaderWarmupComponent))]
public class LoadingSceneManager : MonoBehaviour
{
    public string mainSceneName = "Main";
    public float minimumLoadTime = 1.5f;

    public static int PendingLoadSlot = -1;

    private ShaderWarmupComponent shaderWarmup;

    private void Awake()
    {
        shaderWarmup = GetComponent<ShaderWarmupComponent>();
    }

    private void Start()
    {
        StartCoroutine(LoadWhenReady());
    }

    private IEnumerator LoadWhenReady()
    {
        float elapsed = 0f;

        yield return null;

        while (true)
        {
            elapsed += Time.deltaTime;

            bool queueDone = WarmupManager.Instance == null ||
                             WarmupManager.Instance.IsQueueEmpty;
            bool timerDone = elapsed >= minimumLoadTime;

            if (queueDone && timerDone)
                break;

            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        int slot = PendingLoadSlot;
        PendingLoadSlot = -1;

        AsyncOperation op = SceneManager.LoadSceneAsync(mainSceneName);
        yield return op;

        if (SaveSystem.IsValidSlot(slot))
        {
            LoadResult result = SaveSystem.Load(slot);
            if (result == LoadResult.Failed)
                Debug.LogWarning("Failed to load slot " + slot);
        }
    }
}