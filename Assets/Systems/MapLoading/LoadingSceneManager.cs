using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(ShaderWarmupComponent))]
public class LoadingSceneManager : MonoBehaviour
{
    public string mainSceneName = "Main";
    public float minimumLoadTime = 1.5f;

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
        SceneManager.LoadScene(mainSceneName);
    }
}