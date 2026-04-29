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
    TeleportAiming,
    Teleport,
    Grab,
    Release,
    OrbPlaced,
    DoorOpen,
    DoorClose,
    WirePlug,
    SimonButtonPress,
    SimonCorrect,
    SimonWrong,
    SimonComplete,
    SlidePanel,
    ProductionAmbience,
    ConveyorBelt,
    Assembly,
    AssemblyComplete,
    FuseInsert,
    ValveActivated,
    ValveTurn,
    WireComplete,
    LeverActivate,
    FactoryMachine,
    EndingMusic,
    Typing,
    Notification,
    LightOn,
    HomerInvalidTarget,
    FuseComplete,
    Hover
}

public class AudioManager : MonoBehaviour
{
    private static WaitForSeconds waitForSeconds0_1 = new(0.1f);
    [SerializeField] AudioSource audioSourcePrefab;
    [SerializeField] int maxAudioSourcesCount = 100;
    [SerializeField] List<AudioSource> availableAudioSources = new();
    [Header("Audio Clips")]
    public Sfx[] sfxs;
    public string hasd;
    
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

    public static void PlaySound(SfxType sfx, Transform tf, bool parented = false, bool twoD = false)
    {
        var sfxType = instance.sfxs[(int)sfx];
        if (sfxType.clips == null || sfxType.clips.Length == 0)
        {
            Debug.LogWarning($"Attempted to play SFX of type {sfx} which has no audio clips assigned.");
            return;
        }

        var clip = sfxType.clips[0];
        if (sfxType.clips.Length > 1)
        {
            clip = sfxType.clips[Random.Range(0, sfxType.clips.Length)];
        }
        if(clip == null)
        {
            Debug.LogWarning($"Attempted to play SFX of type {sfx} which has a null audio clip assigned.");
            return;
        }

        // In case an audio source was destroyed or became null, we should clean it up from the pool to avoid errors when trying to access it.
        if (instance.availableAudioSources.Any(item => item == null || item.gameObject == null))
        {
            Debug.LogWarning("Cleaning up null audio sources from the pool.");
            instance.availableAudioSources.RemoveAll(item => item == null || item.gameObject == null);
        }

        var audioSource = instance.availableAudioSources.Find(source => !source.isPlaying);
        
        if (audioSource == null || audioSource.gameObject == null)
        {
            audioSource = instance.AddAudioSource();
            print($"Created new audio source for {clip.name}.");
        }
        
        audioSource.transform.position = tf.position;
        if (parented)
        {
            audioSource.transform.parent = tf;
        }
        else if (audioSource.transform.parent != instance.transform)
        {
            audioSource.transform.parent = tf;
        }

        audioSource.spatialBlend = twoD ? 0f : 1f;
        audioSource.clip = clip;
        audioSource.volume = sfxType.volume;
        audioSource.Play();
#if UNITY_EDITOR
        audioSource.name = $"AudioSource_{sfx}_{clip.name}";
#endif
    }

    public static AudioSource PlayLoopSound(SfxType sfx, Transform tf, bool parented = false, bool twoD = false)
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
        if (clip == null)
        {
            Debug.LogWarning($"Attempted to play looping SFX of type {sfx} which has a null audio clip assigned.");
            return null;
        }

        var audioSource = instance.availableAudioSources.Find(source => !source.isPlaying);
        
        if (audioSource == null || audioSource.gameObject == null)
        {
            audioSource = instance.AddAudioSource();
            print($"Created new audio source for {clip.name}.");
        }

        audioSource.transform.position = tf.position;
        if (parented)
        {
            audioSource.transform.parent = tf;
        }
        else if (audioSource.transform.parent != instance.transform)
        {
            audioSource.transform.parent = tf;
        }

        audioSource.spatialBlend = twoD ? 0f : 1f;
        audioSource.clip = clip;
        audioSource.volume = sfxType.volume;
        audioSource.loop = true;
        audioSource.Play();
#if UNITY_EDITOR
        audioSource.name = $"AudioSource_{sfx}_{clip.name}";
#endif
        return audioSource;
    }

    public static void StopLoopSound(AudioSource source)
    {
        if (source != null && source.isPlaying)
        {
            instance.StartCoroutine(instance.StopSound(source));
        }
    }

    IEnumerator StopSound(AudioSource source)
    {
        if (source == null) yield return null;
        source.transform.parent = transform;
        
        source.volume = 0.0001f; // Avoid clipping sounds when stopping.
        yield return waitForSeconds0_1;
        source.loop = false;
        source.Stop();
    }

    static public void PlayFadeInSound(SfxType sfx, float fadeTime, bool twoD = false)
    {
        var music = instance.sfxs[(int)sfx];
        if (music.clips == null || music.clips.Length == 0)
        {
            Debug.LogWarning($"Attempted to play SFX of type {sfx} which has no audio clips assigned.");
            return;
        }

        var clip = music.clips[0];
        if (music.clips.Length > 1)
        {
            clip = music.clips[Random.Range(0, music.clips.Length)];
        }
        if (clip == null)
        {
            Debug.LogWarning($"Attempted to play SFX of type {sfx} which has a null audio clip assigned.");
            return;
        }

        // In case an audio source was destroyed or became null, we should clean it up from the pool to avoid errors when trying to access it.
        if (instance.availableAudioSources.Any(item => item == null || item.gameObject == null))
        {
            Debug.LogWarning("Cleaning up null audio sources from the pool.");
            instance.availableAudioSources.RemoveAll(item => item == null || item.gameObject == null);
        }

        var audioSource = instance.availableAudioSources.Find(source => !source.isPlaying);

        if (audioSource == null || audioSource.gameObject == null)
        {
            audioSource = instance.AddAudioSource();
            print($"Created new audio source for {clip.name}.");
        }

        audioSource.spatialBlend = twoD ? 0f : 1f;
        audioSource.clip = clip;
        var targetVolume = music.volume;
#if UNITY_EDITOR
        audioSource.name = $"AudioSource_{sfx}_{clip.name}";
#endif

        instance.StartCoroutine(instance.PlayFadeInSoundCoroutine(audioSource, fadeTime, music.volume));
    }

    IEnumerator PlayFadeInSoundCoroutine(AudioSource audioSource, float fadeTime, float targetVolume)
    {
        audioSource.volume = 0f;
        audioSource.Play();
        var t = 0f;
        while (audioSource.volume < targetVolume)
        {                                       
            audioSource.volume = Mathf.Lerp(0f, targetVolume, t / fadeTime);
            t += Time.deltaTime;
            print("Audio source volume: " + audioSource.volume + "| Delta Time: " + t);
            yield return null;
        }
        audioSource.volume = targetVolume;
    }

    static public void PlaySoundRandomPitchAndVolume(SfxType sfx, float pitchRange = 0.1f, float volumeRange = 0.02f, bool twoD = false)
    {
        var sfxType = instance.sfxs[(int)sfx];
        if (sfxType.clips == null || sfxType.clips.Length == 0)
        {
            Debug.LogWarning($"Attempted to play SFX of type {sfx} which has no audio clips assigned.");
            return;
        }
        var clip = sfxType.clips[0];
        if (sfxType.clips.Length > 1)
        {
            clip = sfxType.clips[Random.Range(0, sfxType.clips.Length)];
        }
        if (clip == null)
        {
            Debug.LogWarning($"Attempted to play SFX of type {sfx} which has a null audio clip assigned.");
            return;
        }

        // In case an audio source was destroyed or became null, we should clean it up from the pool to avoid errors when trying to access it.
        if (instance.availableAudioSources.Any(item => item == null || item.gameObject == null))
        {
            Debug.LogWarning("Cleaning up null audio sources from the pool.");
            instance.availableAudioSources.RemoveAll(item => item == null || item.gameObject == null);
        }

        var audioSource = instance.availableAudioSources.Find(source => !source.isPlaying);

        if (audioSource == null || audioSource.gameObject == null)
        {
            audioSource = instance.AddAudioSource();
            print($"Created new audio source for {clip.name}.");
        }

        audioSource.spatialBlend = twoD ? 0f : 1f;
        audioSource.pitch = 1 + Random.Range(-pitchRange, pitchRange);
        audioSource.clip = clip;
        audioSource.volume = sfxType.volume + Random.Range(-volumeRange, volumeRange);
        audioSource.Play();
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