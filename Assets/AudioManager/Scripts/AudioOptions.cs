using UnityEngine;
using UnityEngine.UI;

public class AudioOptions : MonoBehaviour
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
        MusicToggle.isOn = AudioManager.Controller.MusicOn;
        SoundFxToggle.isOn = AudioManager.Controller.SoundFxOn;

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

