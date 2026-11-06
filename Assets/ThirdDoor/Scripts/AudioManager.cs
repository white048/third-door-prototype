using UnityEngine;

public enum GameSound
{
    LetterShatter,
    LockedThud,
    DoorOpenSuccess,
    DoorCloseBad,
    ChoiceClick,
    AmbientDrone
}

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;

    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioManager>();
            }
            return _instance;
        }
    }

    private AudioSource oneShotSource;
    private AudioSource loopSource;
    private AudioClip footstep;
    private AudioLowPassFilter roomFilter;

    // Filenames the user drops into Assets/Resources/Audio/ (without extension).
    // Missing clips are tolerated — code logs a one-time warning and continues silently.
    private static readonly System.Collections.Generic.Dictionary<GameSound, string> ClipNames = new System.Collections.Generic.Dictionary<GameSound, string>
    {
        { GameSound.LetterShatter,   "letter_shatter" },
        { GameSound.LockedThud,      "locked_thud" },
        { GameSound.DoorOpenSuccess, "door_open_success" },
        { GameSound.DoorCloseBad,    "door_close_bad" },
        { GameSound.ChoiceClick,     "choice_click" },
        { GameSound.AmbientDrone,    "ambient_drone" }
    };

    private System.Collections.Generic.Dictionary<GameSound, AudioClip> clips;

    private void Awake()
    {
        _instance = this;
        clips = new System.Collections.Generic.Dictionary<GameSound, AudioClip>();
        foreach (var kvp in ClipNames)
        {
            AudioClip clip = Resources.Load<AudioClip>("Audio/" + kvp.Value);
            if (clip != null)
            {
                clips[kvp.Key] = clip;
            }
        }

        oneShotSource = gameObject.AddComponent<AudioSource>();
        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;
        oneShotSource.spatialBlend = 0f; // 2D

        var room = new GameObject("Room tone");
        room.transform.SetParent(transform, false);
        loopSource = room.AddComponent<AudioSource>();
        roomFilter = room.AddComponent<AudioLowPassFilter>();
        loopSource.playOnAwake = false;
        loopSource.loop = true;
        loopSource.spatialBlend = 0f;
        loopSource.volume = 0.35f;
        // A restrained, deterministic paper/concrete footfall, generated without another asset dependency.
        const int rate = 22050;
        float[] samples = new float[2205];
        var noise = new System.Random(37);
        float low = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / rate;
            low = Mathf.Lerp(low, (float)noise.NextDouble() * 2 - 1, .14f);
            samples[i] = (low * .5f + Mathf.Sin(t * 2 * Mathf.PI * 96) * .25f) * Mathf.Exp(-t * 48);
        }
        footstep = AudioClip.Create("Gallery footfall", samples.Length, 1, rate, false);
        footstep.SetData(samples, 0);
    }

    public void Play(GameSound sound, float volume = 1f)
    {
        if (clips != null && clips.TryGetValue(sound, out AudioClip clip) && oneShotSource != null)
        {
            oneShotSource.PlayOneShot(clip, volume);
        }
    }

    public void PlayLoop(GameSound sound, float volume = 0.35f)
    {
        if (clips != null && clips.TryGetValue(sound, out AudioClip clip) && loopSource != null)
        {
            loopSource.clip = clip;
            loopSource.volume = volume;
            loopSource.Play();
        }
    }

    public void StopLoop()
    {
        if (loopSource != null)
        {
            loopSource.Stop();
        }
    }

    public void PlayFootstep()
    {
        if (oneShotSource != null && footstep != null) oneShotSource.PlayOneShot(footstep, .2f);
    }

    public void SetPressure(float load, float breath)
    {
        if (loopSource == null) return;
        loopSource.pitch = Mathf.Lerp(loopSource.pitch, 1f - load * .1f, Time.deltaTime);
        loopSource.volume = Mathf.Lerp(loopSource.volume, .13f + load * .07f, Time.deltaTime);
        if (roomFilter != null) roomFilter.cutoffFrequency = Mathf.Lerp(2500, 18000, breath);
    }

    private void OnDestroy()
    {
        if (footstep != null) Destroy(footstep);
        if (_instance == this) _instance = null;
    }
}
