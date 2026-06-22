using UnityEngine;
using System.Collections.Generic;

public class UISoundManager : MonoBehaviour
{
    public static UISoundManager instance;

    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
    }

    public AudioSource uiSource;

    public List<Sound> sounds = new List<Sound>();
    private Dictionary<string, AudioClip> soundMap;

    void Awake()
    {
        instance = this;

        soundMap = new Dictionary<string, AudioClip>();

        foreach (var s in sounds)
        {
            soundMap[s.name] = s.clip;
        }
    }

    public void Play(string soundName)
    {
        if (soundMap.TryGetValue(soundName, out AudioClip clip))
        {
            uiSource.PlayOneShot(clip);
        }
    }
}