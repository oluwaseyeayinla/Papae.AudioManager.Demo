using Papae2D.AudioEngine;
using UnityEngine;
using UnityEngine.UI;


public class SoundSettings : MonoBehaviour
{
    public Toggle MusicToggle, SoundFxToggle;

    void OnEnable()
    {
        if (!MusicToggle || !SoundFxToggle)
        {
            Debug.LogError("Please assign the neccesary toggle variables in the inspector", this);
            return;
        }

        // set Toggle Properties
        MusicToggle.isOn = AudioManager.Options.musicOn;
        SoundFxToggle.isOn = AudioManager.Options.soundFxOn;

        // add Listeners
        MusicToggle.onValueChanged.AddListener(ToggleBGMusic);
        SoundFxToggle.onValueChanged.AddListener(ToggleSoundFx);
    }

    void OnDisable()
    {
        // remove Listeners
        MusicToggle.onValueChanged.RemoveAllListeners();
        SoundFxToggle.onValueChanged.RemoveAllListeners();
    }

    public void ToggleBGMusic(bool flag)
    {
        AudioManager.ToggleBGMMute(flag);
    }

    public void ToggleSoundFx(bool flag)
    {
        AudioManager.ToggleSFXMute(flag);
    }
}

