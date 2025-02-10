/*
using UnityEngine;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    [HideInInspector] public AudioSource source;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private List<Sound> sounds = new List<Sound>();
    private void Start(){

        //StartCoroutine(PlayLaserGunSound());
        
    }
    private System.Collections.IEnumerator PlayLaserGunSound()
    {
        while (true)
        {
            AudioManager.Instance.Play("laser_impact_01");
            yield return new WaitForSeconds(3f);
        }
    }    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
        }
    }
    private void OnValidate()
    {
        Debug.Log("Updating vol");
        // Ensure changes in Inspector are applied immediately
        foreach (Sound s in sounds)
        {
            if (s.source != null)
            {
                s.source.volume = s.volume;
                s.source.pitch = s.pitch;
            }
        }
    }
    public void Play(string soundName)
    {
        Debug.Log($"Playing Sound {soundName}");
        Sound s = sounds.Find(sound => sound.name == soundName);
        if (s != null)
        {
            s.source.Play();
        }
        else
        {
            Debug.LogWarning($"AudioManager: Sound '{soundName}' not found!");
        }
    }

    public void Stop(string soundName)
    {
        Sound s = sounds.Find(sound => sound.name == soundName);
        if (s != null)
        {
            s.source.Stop();
        }
    }
}
*/

using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Sound
{
    public string name;
    public List<AudioClip> clips = new List<AudioClip>(); // Multiple clips per sound
    [Range(0f, 4f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    public bool loop = false;
    [HideInInspector] public List<AudioSource> sources = new List<AudioSource>(); // List of sources
}


public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private List<Sound> sounds = new List<Sound>();
    private void Start(){

        //StartCoroutine(PlayLaserGunSound());
        AudioManager.Instance.Play("music_01");
    }
    private System.Collections.IEnumerator PlayLaserGunSound()
    {
        while (true)
        {
            AudioManager.Instance.Play("laser_impact_01");
            yield return new WaitForSeconds(1.5f);
        }
    }  
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        foreach (Sound s in sounds)
        {
            s.sources.Clear(); // Ensure it's empty before populating

            foreach (AudioClip clip in s.clips)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.clip = clip;
                source.volume = s.volume;
                source.pitch = s.pitch;
                source.loop = s.loop; 
                s.sources.Add(source);
            }
        }
    }

    private void OnValidate()
    {
        foreach (Sound s in sounds)
        {
            foreach (AudioSource source in s.sources)
            {
                if (source != null)
                {
                    source.volume = s.volume;
                    source.pitch = s.pitch;
                    source.loop = s.loop; 
                }
            }
        }
    }

    public void Play(string soundName)
    {
        Sound s = sounds.Find(sound => sound.name == soundName);
        if (s != null && s.sources.Count > 0)
        {
            AudioSource sourceToPlay = s.sources[Random.Range(0, s.sources.Count)];
            sourceToPlay.Play();
        }
        else
        {
            Debug.LogWarning($"AudioManager: Sound '{soundName}' not found or has no clips!");
        }
    }

    public void Stop(string soundName)
    {
        Sound s = sounds.Find(sound => sound.name == soundName);
        if (s != null)
        {
            foreach (AudioSource source in s.sources)
            {
                source.Stop();
            }
        }
    }
}
