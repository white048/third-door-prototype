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

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.playOnAwake = false;
        loopSource.loop = true;
        loopSource.spatialBlend = 0f;
        loopSource.volume = 0.35f;
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
}
