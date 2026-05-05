using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using UnityEngine.Tilemaps;
using UnityEditor;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;

[Serializable]
public class Sfx
{
    public AudioClip[] clips;
    [Range(0, 1)] public float volume;
}

// For new SFX types, add a new entry to the SfxType enum and Create a new SfxScriptableObject for it.
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
    SimonSequenceLight,
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
    Hover,
    Griddy
}

public class AudioManager : MonoBehaviour
{
    static WaitForSeconds waitForSeconds0_1 = new(0.1f);
    [SerializeField] AudioSource audioSourcePrefab;
    [SerializeField] int maxAudioSourcesCount = 100;
    [SerializeField] Queue<AudioSource> audioSourcePool = new();

    [Header("Audio Clips")]
    [SerializeField] SfxScriptableObject[] sfxScriptableObject;
    Dictionary<SfxType, SfxScriptableObject> sfxDictionary = new();

    Coroutine pruneRoutine;
    static AudioManager instance;
    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<AudioManager>();

                if (instance == null)
                {
                    Debug.LogError("No AudioManager in scene!");
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);

        // Clean up any existing audio sources.
        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        PopulateDictionary();
        CreateAudioSourcePool();
        pruneRoutine = StartCoroutine(PrunePool());
    }

    void PopulateDictionary()
    {
        sfxDictionary.Clear();
        foreach (var sfx in sfxScriptableObject)
        {
            if(sfx == null)
            {
                Debug.LogWarning($"Null SfxScriptableObject found at index {sfx} in AudioManager inspector. Assign a valid SfxScriptableObject or remove the entry.");
                continue;
            }
            if(sfxDictionary.ContainsKey(sfx.sfxType))
            {
                Debug.LogWarning($"Duplicate SfxType {sfx.sfxType} found in AudioManager inspector. Remove or change the duplicate entry.");
                continue;
            }
            sfxDictionary.Add(sfx.sfxType, sfx);
        }
    }

    /// <summary>
    /// Initializes the audio source pool by instantiating a specified number of audio sources from the prefab and adding them to the pool.
    /// </summary>
    void CreateAudioSourcePool()
    {
        for (int i = 0; i < maxAudioSourcesCount; i++)
        {
            var audioSource = Instantiate(audioSourcePrefab, transform);
            audioSource.gameObject.name = $"AudioSource_{i}";
            audioSourcePool.Enqueue(audioSource);
        }
    }

    IEnumerator PrunePool()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f);
            while (audioSourcePool.Count > maxAudioSourcesCount)
            {
                var audioSource = audioSourcePool.Dequeue();
                Destroy(audioSource.gameObject);
            }
        }
    }

    /// <summary>
    /// If there are no available audio sources to play a clip, this method can be called to add more audio sources to the pool.
    /// </summary>
    /// <returns></returns>
    AudioSource AddAudioSource()
    {
        var audioSource = Instantiate(audioSourcePrefab, transform);
        audioSource.gameObject.name = $"AudioSource_{audioSourcePool.Count}";
        audioSourcePool.Enqueue(audioSource);
        return audioSource;
    }

    /// <summary>
    /// If there are available audio sources in the pool, this method will return one for use. 
    /// If the pool is empty, it will call AddAudioSource to create a new audio source and return it.
    /// </summary>
    AudioSource GetAudioSource()
    {
        if (audioSourcePool.Count == 0)
            AddAudioSource();
        return audioSourcePool.Dequeue();
    }

    /// <summary>
    /// Resets the specified AudioSource to default settings and returns it to the pool for reuse.
    /// </summary>
    IEnumerator ReturnAudioSource(AudioSource source)
    {
        yield return new WaitForSeconds(source.clip.length);
        source.clip = null;
        source.loop = false;
        source.volume = 1f;
        source.pitch = 1f;
        source.spatialBlend = 1f;
        source.transform.SetParent(transform, false);
        audioSourcePool.Enqueue(source);
    }

    /// <summary>
    /// Try to get a random audio clip for the specified SFX type. 
    /// If there are no clips assigned for that SFX type, it will log a warning and return false. Otherwise, it will return true and output the randomly selected clip.
    /// </summary>
    bool TryGetClip(SfxType sfxType, out AudioClip clip, out float volume)
    {
        clip = null;
        var sfx = sfxDictionary.GetValueOrDefault(sfxType);
        volume = sfx.data.volume;
        if (sfx == null || sfx.data.clips == null || sfx.data.clips.Length == 0)
        {
            Debug.LogWarning($"No clips assigned for SFX: {sfxType}");
            return false;
        }

        clip = sfx.data.clips[Random.Range(0, sfx.data.clips.Length)];
        return clip != null;
    }

    /// <summary>
    /// Parents the audio source to the target transform if parented is true, otherwise it parents it to the AudioManager.
    /// </summary>
    void SetAudioSourceTransform(AudioSource audioSource, Transform targetTransform, bool parented)
    {
        if (targetTransform == null)
        {
            audioSource.transform.SetParent(transform, false);
            return;
        }
        audioSource.transform.SetParent(parented ? targetTransform : transform, false);
        audioSource.transform.position = targetTransform.position;
    }

    /// <summary>
    /// Used to avoid clipping sounds when stopping.
    /// </summary>
    IEnumerator StopSound(AudioSource source)
    {
        if (source == null) yield return null;
        source.volume = 0.0001f; // Avoid clipping sounds when stopping.
        yield return waitForSeconds0_1;
        source.Stop();
        ReturnAudioSource(source);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        PopulateDictionary();
    }

    public static void EditorTestPlay(SfxType sfx)
    {
        if (instance == null)
        {
            instance = FindFirstObjectByType<AudioManager>();
            if (instance == null)
            {
                Debug.LogError("No AudioManager in scene!");
                return;
            }
        }
        Play(sfx, Camera.main.transform);
    }
#endif

        /// <summary>
        /// Plays a sound effect at the position of the specified transform. 
        /// If parented is true, the audio source will be parented to the transform and will move with it. 
        /// If twoD is true, the sound will be played as a 2D sound (not affected by spatialization). Otherwise, it will be played as a 3D sound.
        /// </summary>
    public static void Play(SfxType sfx, Transform tf = null, bool parented = false, bool twoD = false)
    {
        if (!instance.TryGetClip(sfx, out var clip, out float volume)) return;

        var audioSource = instance.GetAudioSource();

        instance.SetAudioSourceTransform(audioSource, tf, parented);

        audioSource.clip = clip;
        audioSource.spatialBlend = twoD ? 0f : 1f;
        audioSource.volume = volume;
        audioSource.Play();
        instance.StartCoroutine(instance.ReturnAudioSource(audioSource));
#if UNITY_EDITOR
        audioSource.name = $"AudioSource_{sfx}_{clip.name}";
#endif
    }

    /// <summary>
    /// Plays a looping sound effect at the position of the specified transform.
    /// To stop the looping sound, call StopLoopSound and pass in the AudioSource returned by this method.
    /// </summary>
    public static AudioSource PlayLooping(SfxType sfx, Transform tf = null, bool parented = false, bool twoD = false)
    {
        if (!instance.TryGetClip(sfx, out var clip, out float volume)) return null;

        var audioSource = instance.GetAudioSource();

        instance.SetAudioSourceTransform(audioSource, tf, parented);

        audioSource.clip = clip;
        audioSource.spatialBlend = twoD ? 0f : 1f;
        audioSource.volume = volume;
        audioSource.loop = true;
        audioSource.Play();
#if UNITY_EDITOR
        audioSource.name = $"AudioSource_{sfx}_{clip.name}";
#endif
        return audioSource;
    }

    /// <summary>
    /// Used to stop a looping sound that was started with PlayLoopSound. 
    /// The AudioSource passed in should be the one returned by PlayLoopSound when the looping sound was started.
    /// </summary>
    public static void StopLooping(AudioSource source)
    {
        if (source != null && source.isPlaying)
        {
            instance.StartCoroutine(instance.StopSound(source));
        }
    }

    /// <summary>
    /// Plays a sound effect with a fade-in effect over the specified fade time.
    /// </summary>
    static public void PlayFadeIn(SfxType sfx, float fadeTime = 1f, bool twoD = false)
    {
        if (!instance.TryGetClip(sfx, out var clip, out float volume)) return;

        var audioSource = instance.GetAudioSource();

        audioSource.clip = clip;
        audioSource.spatialBlend = twoD ? 0f : 1f;
        audioSource.volume = volume;
        instance.StartCoroutine(instance.PlayFadeCoroutine(audioSource, fadeTime, 0, audioSource.volume));
        instance.StartCoroutine(instance.ReturnAudioSource(audioSource));
#if UNITY_EDITOR
        audioSource.name = $"AudioSource_{sfx}_{clip.name}";
#endif
    }

    /// <summary>
    /// Fades the sound effect's volume from the specified start volume to the target volume over the specified fade time.
    /// </summary>
    /// <returns></returns>
    IEnumerator PlayFadeCoroutine(AudioSource audioSource, float fadeTime, float startVolume, float targetVolume)
    {
        audioSource.volume = startVolume;
        audioSource.Play();
        var t = 0f;
        while (t < fadeTime)
        {
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, t / fadeTime);
            t += Time.deltaTime;
            print("Audio source volume: " + audioSource.volume + "| Delta Time: " + t);
            yield return null;
        }
        audioSource.volume = targetVolume;
    }

    /// <summary>
    /// Plays a sound effect with random pitch and volume variations.
    /// </summary>
    static public void PlayRandomPitchAndVolume(SfxType sfx, float pitchRange = 0.1f, float volumeRange = 0.02f, bool twoD = false)
    {
        if (!instance.TryGetClip(sfx, out var clip, out float volume)) return;

        var audioSource = instance.GetAudioSource();

        if (audioSource == null || audioSource.gameObject == null)
        {
            audioSource = instance.AddAudioSource();
            print($"Created new audio source for {clip.name}.");
        }

        audioSource.clip = clip;
        audioSource.spatialBlend = twoD ? 0f : 1f;
        audioSource.pitch = 1 + Random.Range(-pitchRange, pitchRange);
        audioSource.volume = audioSource.volume = volume + Random.Range(-volumeRange, volumeRange);
        audioSource.Play();
        instance.StartCoroutine(instance.ReturnAudioSource(audioSource));
#if UNITY_EDITOR
        audioSource.name = $"AudioSource_{sfx}_{clip.name}";
#endif
    }
}

[CustomEditor(typeof(AudioManager))]
public class AudioManagerEditor : Editor
{
    SfxType sfxType;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        sfxType = (SfxType)EditorGUILayout.EnumPopup("SFX", sfxType);
        if (GUILayout.Button("Test SFX"))
        {
            Debug.Log($"Playing test sfx: {sfxType}");
            AudioManager.EditorTestPlay(sfxType);
        }
    }
}