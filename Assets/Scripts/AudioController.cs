using UnityEngine;
using UnityEngine.Audio;

[System.Serializable]
public class AudioController : MonoBehaviour
{
    // default or starting volume of the background music
    public static readonly float DefaultMusicVol = 0.12f;
    // default or starting volume of the sound effects
    public static readonly float DefaultSFxVol = 1.0f;

    /// <summary>
    /// if none is to be used, then leave unassigned or blank
    /// </summary>
    public AudioMixer MasterMixer;

    /// true if the music is enabled	
    public bool MusicOn = true;
    /// true if the sound fx are enabled
    public bool SoundFxOn = true;

    /// the music volume
    [Range(0, 1)]
    public float MusicVolume = DefaultMusicVol;
    /// the sound fx volume
    [Range(0, 1)]
    public float SoundFxVolume = DefaultSFxVol;

    /// true if both SoundFxOn and MusicOn are false
    public bool IsMute
    {
        get { return !MusicOn && !SoundFxOn; }
    }
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

[System.Serializable]
public struct AbstractAudioSnapshot
{
    public string Name;
    public AudioMixerSnapshot SnapShot;
}

[System.Serializable]
public struct AbstractBackgroundMusic
{
    public AudioClip CurrentClip;
    public AudioClip NextClip;
    public MusicTransition Transition;
}

[System.Serializable]
public struct AbstractLoopingSoundAsset
{
    public string Name;
    public AudioClip Clip;
    public float Duration;

    public AbstractLoopingSoundAsset(string name, AudioClip clip, float duration)
    {
        Name = name;
        Clip = clip;
        Duration = duration;
    }
}

[System.Serializable]
public struct AudioAsset
{
    public string Name;
    public AudioClip Clip;
}
