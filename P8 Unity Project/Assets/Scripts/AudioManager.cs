using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

// For new SFX types, add a new entry to the SfxType enum and then assign audio clips to the new SFX type in the AudioManager inspector.
public enum SfxType
{
    Explosion,
    Fire,
    ArmAttach,
    Grab,
    Release,
    ButtonPress,
    PuzzleComplete,
    DoorOpen,
    DoorClose,
    Plug,
    SlidePanel,
    ProductionAmbience,
    ConveyorBelt,
    Assembly,
    AssemblyComplete,
}

public class AudioManager : MonoBehaviour
{
    [SerializeField] AudioSource audioSourcePrefab;
    [SerializeField] int maxAudioSourcesCount = 100;
    [SerializeField] List<AudioSource> availableAudioSources = new();
    [Header("Audio Clips")]
    public Sfx[] sfxs;
    
    public static AudioManager instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
        CreateAudioSourcePool();
    }

    void CreateAudioSourcePool()
    {
        for (int i = 0; i < maxAudioSourcesCount; i++ ) {
            var audioSource = Instantiate(audioSourcePrefab, transform);
            audioSource.gameObject.name = $"AudioSource_{i}";
            availableAudioSources.Add(audioSource);
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Assign SFX names in the inspector based on the SfxType enum.
        string[] sfxNames = Enum.GetNames(typeof(SfxType));
        Array.Resize(ref sfxs, sfxNames.Length);
        for (int i = 0; i < sfxNames.Length; i++)
        {
            if (sfxs[i] == null)
            {
                sfxs[i] = new Sfx();
            }
            sfxs[i].title = sfxNames[i];
        }
    }
#endif

    public static void PlaySound(SfxType sfx, Vector3 location, float volume = 1f, bool parented = false)
    {
        var sfxType = instance.sfxs[(int)sfx];
        if (sfxType.clips == null || sfxType.clips.Length == 0)
        {
            Debug.LogWarning($"Attempted to play SFX of type {sfx} which has no audio clips assigned.");
            return;
        }

        var clipToPlay = sfxType.clips[0];
        if (sfxType.clips.Length > 1)
        {
            clipToPlay = sfxType.clips[Random.Range(0, sfxType.clips.Length)];
        }

        // In case an audio source was destroyed or became null, we should clean it up from the pool to avoid errors when trying to access it.
        if (instance.availableAudioSources.Any(item => item == null || item.gameObject == null))
        {
            print("Cleaning up null audio sources from the pool.");
            instance.availableAudioSources.RemoveAll(item => item == null || item.gameObject == null);
        }

        var clipLength = clipToPlay.length;
        var audioSource = instance.availableAudioSources.Find(source => !source.isPlaying);
        
        if (audioSource == null || audioSource.gameObject == null)
        {
            audioSource = instance.AddAudioSource();
            print($"Created new audio source for {clipToPlay.name}.");
        }

        audioSource.transform.position = location;
        
        if (parented)
        {
            audioSource.transform.parent.position = location;
        }
        audioSource.clip = clipToPlay;
        audioSource.volume = volume;
        audioSource.Play();
        
        instance.StartCoroutine(instance.StopSound(audioSource, clipLength + 0.1f)); // Add a small buffer to ensure the clip has finished playing before relisting the audio source.
    }

    IEnumerator StopSound(AudioSource source, float delay = 0)
    {
        yield return new WaitForSeconds(delay);
        if (source == null) yield return null;
        source.transform.parent = transform;
        
        source.volume = 0.0001f; // Avoid clipping sounds when stopping.
        yield return new WaitForSeconds(0.1f);
        source.Stop();
    }

    public static AudioSource PlayLoopSound(SfxType sfx, Vector3 location, float volume = 1f, bool parented = false)
    {
        var sfxType = instance.sfxs[(int)sfx];
        if (sfxType.clips == null || sfxType.clips.Length == 0)
        {
            Debug.LogWarning($"Attempted to play SFX of type {sfx} which has no audio clips assigned.");
            return null;
        }
        var clip = sfxType.clips[0];
        // In case an audio source was destroyed or became null, we should clean it up from the pool to avoid errors when trying to access it.
        if (instance.availableAudioSources.Any(item => item == null || item.gameObject == null))
        {
            print("Cleaning up null audio sources from the pool.");
            instance.availableAudioSources.RemoveAll(item => item == null || item.gameObject == null);
        }

        var audioSource = instance.availableAudioSources.Find(source => !source.isPlaying);
        
        if (audioSource == null || audioSource.gameObject == null)
        {
            audioSource = instance.AddAudioSource();
            print($"Created new audio source for {clip.name}.");
        }

        audioSource.transform.position = location;

        if (parented)
        {
            audioSource.transform.parent.position = location;
        }
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.loop = true;
        audioSource.Play();

        return audioSource;
    }

    public static void StopLoopSound(AudioSource source)
    {
        if (source != null && source.isPlaying)
        {
            source.transform.parent = instance.transform;
            instance.StartCoroutine(instance.StopSound(source));
        }
    }

    /// <summary>
    /// If there are no available audio sources to play a clip, this method can be called to add more audio sources to the pool.
    /// </summary>
    /// <returns></returns>
    AudioSource AddAudioSource() 
    {
        var audioSource = Instantiate(audioSourcePrefab, transform);
        audioSource.gameObject.name = $"AudioSource_{availableAudioSources.Count}";
        availableAudioSources.Add(audioSource);
        return audioSource;
    }
}

[Serializable]
public class Sfx
{
#if UNITY_EDITOR
    [HideInInspector] public string title;
#endif
    public AudioClip[] clips;
    [Range(0, 1)] public float volume;
}