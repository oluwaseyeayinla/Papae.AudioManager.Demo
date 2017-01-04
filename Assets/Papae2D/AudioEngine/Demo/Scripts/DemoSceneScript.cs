using Papae2D.AudioEngine;
using UnityEngine;
using UnityEngine.UI;


public class DemoSceneScript : MonoBehaviour
{
    [Header("Controller Properties")]
    public Slider MusicSlider;
    public Slider SoundFxSlider;

    [Header("Effects Properties")]
    public Slider RepeatSlider;
    public Text RepeatText;
    public Slider CallbackSlider;


    // reference to the audiosource for the repeat clip
    AudioSource repeatSource = null;
    // reference to the aaudiosource for the callback clip
    AudioSource callbackSource = null;
    // total time for the repat sound
    float totalTime;
    // pool index where repeat information is stored
    int repeatIndex;


    void Start()
    {
        // retrieve the first background music and play it
        AudioClip clip = AudioManager.GetClipFromAssetList("MenuMusic");
        AudioManager.PlayBGM(clip, MusicTransition.Swift);

        // set the default values for the controller properties 
        MusicSlider.value = AudioManager.Options.musicVolume;
        SoundFxSlider.value = AudioManager.Options.soundFxVolume;

        // set the default calues for the effects properties
        RepeatSlider.value = 0;
        RepeatText.text = "x" + 0;
        CallbackSlider.value = 0;
    }

    void LateUpdate()
    {
        // update the volume of the sliders | this also helps if you change the volume from the controller or mixer
        MusicSlider.value = AudioManager.Options.musicVolume;
        SoundFxSlider.value = AudioManager.Options.soundFxVolume;

        // update the sider properties if the repeat sound source exists
        if (repeatSource != null)
        {
            // get index where repeat sound is stored
            repeatIndex = AudioManager.IndexOfRepeatingPool(AudioManager.GetClipFromAssetList("TickTock").name);

            // update value if repeat sound exists
            if (repeatIndex >= 0)
            {
                // remainder of duration left
                RepeatSlider.value = (AudioManager.RepeatSoundPool[repeatIndex].duration / totalTime) * 1;
                // remainder of repeats left
                RepeatText.text = "x" + (Mathf.RoundToInt((AudioManager.RepeatSoundPool[repeatIndex].duration / repeatSource.clip.length)));
            }
        }

        // update the sider properties if the callback sound source exists
        if (callbackSource != null)
        {
            //Debug.Log("Playback Pos: " + callbackSource.time);
            //Debug.Log("Playback Len: " + callbackSource.clip.length);
            CallbackSlider.value = 1 - (callbackSource.time / callbackSource.clip.length);
        }
    }

    public void SetMusicVolume(float value)
    {
        AudioManager.SetBGMVolume(value);
    }

    public void SetSoundEffectsVolume(float value)
    {
        AudioManager.SetSFXVolume(value);
    }

    // gets the next background clip based on the current one
    AudioClip GetNextBackgroundMusicClip()
    {
        if (AudioManager.BGM.currentClip == AudioManager.GetClipFromAssetList("MenuMusic"))
        {
            return AudioManager.GetClipFromAssetList("GameMusic");
        }

        return AudioManager.GetClipFromAssetList("MenuMusic");
    } 

    // swift button function
    public void SwitchBackgroundMusicUsingSwiftTransition()
    {
        AudioClip clip = GetNextBackgroundMusicClip();
        AudioManager.PlayBGM(clip, MusicTransition.Swift);
    }

    // fade button function
    public void SwitchBackgroundMusicUsingFadeTransition()
    {
        AudioClip clip = GetNextBackgroundMusicClip();
        AudioManager.PlayBGM(clip, MusicTransition.LinearFade, 4f);
    }

    // crossfade button function
    public void SwitchBackgroundMusicUsingCrossfadeTransition()
    {
        AudioClip clip = GetNextBackgroundMusicClip();
        AudioManager.PlayBGM(clip, MusicTransition.CrossFade, 4f);
    }

    // one shot button function
    public void PlayOneShotSoundEffect()
    {
        AudioManager.PlayOneShot(AudioManager.GetClipFromAssetList("OneShot"));
    }

    // repeat button function
    public void PlayRepeatSoundEffect(int amount)
    {
        repeatSource = AudioManager.RepeatSFX(AudioManager.GetClipFromAssetList("TickTock"), (uint)Mathf.Abs(amount));
        totalTime = repeatSource.clip.length * amount;
    }

    // callback button function
    public void PlayCallbackSoundEffect()
    {
        callbackSource = AudioManager.PlayOneShot(AudioManager.GetClipFromAssetList("Callback1"), CallbackFunction);
    }

    public void CallbackFunction()
    {
        AudioManager.PlayOneShot(AudioManager.GetClipFromAssetList("Callback2"));
    }
}
