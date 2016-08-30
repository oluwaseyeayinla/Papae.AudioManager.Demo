using System;
using UnityEngine;
using UnityEngine.Audio;

// The regulator of all things with regards to audio
[Serializable]
public class AudioController : MonoBehaviour
{
    /// <summary>
    /// Default volume of the background music
    /// </summary>
    public static readonly float DefaultMusicVol = 0.12f;
    /// <summary>
    /// Default volume of the sound effects
    /// </summary>
    public static readonly float DefaultSFxVol = 1.0f;

    /// <summary>
    /// The target group for the background music to route its their signals
    /// </summary>
    [Tooltip("if none is to be used, then leave unassigned or blank")]
    public AudioMixerGroup MusicMixerGroup;

    /// <summary>
    /// The target group for the sound effects to route its their signals
    /// </summary>
    [Tooltip("if none is to be used, then leave unassigned or blank")]
    public AudioMixerGroup SoundFxMixerGroup;

    /// <summary>
    /// Is the background music mute
    /// </summary>	
    public bool MusicOn = true;

    /// <summary>
    /// Is the sound fx mute
    /// </summary>
    public bool SoundFxOn = true;


    /// <summary>
    /// The background music volume
    /// </summary>
    [Range(0, 1)]
    public float MusicVolume = DefaultMusicVol;

    /// <summary>
    /// The sound fx volume
    /// </summary>
    [Range(0, 1)]
    public float SoundFxVolume = DefaultSFxVol;
}

public class SoundEFfectTag : MonoBehaviour
{

}

public enum MusicTransition
{
    Swift,
    FadeOutFadeIn,
    CrossFade
}

[Serializable]
public struct AudioAsset
{
    public string Name;
    public AudioClip Clip;
}

[Serializable]
public struct BackgroundMusic
{
    public AudioClip CurrentClip;
    public AudioClip NextClip;
    public MusicTransition Transition;
}

[Serializable]
public struct RepeatSound
{
    public string Name;
    public AudioSource Source;
    public float Duration;
    public Action Callback;
}

