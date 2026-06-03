using UnityEngine;

public class DebugSettings : MonoBehaviour
{
    [SerializeField] private int targetFPS = 60;

    void Awake()
    {
        Application.targetFrameRate = targetFPS;
    }
}