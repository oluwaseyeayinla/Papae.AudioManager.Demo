using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DemoSceneScript : MonoBehaviour
{
    [Header("Controller Properties")]
    public Slider MusicSlider;
    public Slider SoundFxSlider;

    [Header("Effects Properties")]
    public Slider RepeatSlider;
    public Text RepeatText;
    public Slider CallbackSlider;


    AudioSource repeatSource = null, callbackSource = null;
    float totalTime, repeatAmount;
    int repeatIndex;


    void Start()
    {
        AudioClip clip = AudioManager.GetClipFromAssetList("BGMusic1");
        AudioManager.PlayBGM(clip, MusicTransition.Swift);

        MusicSlider.value = AudioManager.Controller.MusicVolume;
        SoundFxSlider.value = AudioManager.Controller.SoundFxVolume;

        RepeatSlider.value = 0;
        RepeatText.text = "x" + 0;
        CallbackSlider.value = 0;
    }

    void LateUpdate()
    {
        MusicSlider.value = AudioManager.Controller.MusicVolume;
        SoundFxSlider.value = AudioManager.Controller.SoundFxVolume;

        if (repeatSource != null)
        {
            repeatIndex = AudioManager.GetRepeatingSoundIndex(AudioManager.GetClipFromAssetList("TickTock").name);
            RepeatSlider.value = (AudioManager.RepeatSoundPool[repeatIndex].Duration/ totalTime) * 1;
            RepeatText.text = "x" + (Mathf.RoundToInt((AudioManager.RepeatSoundPool[repeatIndex].Duration / repeatSource.clip.length)));
        }

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
        AudioManager.SetSFxVolume(value);
    }

    AudioClip GetNextBackgroundMusicClip()
    {
        if (AudioManager.BGM.CurrentClip == AudioManager.GetClipFromAssetList("BGMusic1"))
        {
            return AudioManager.GetClipFromAssetList("BGMusic2");
        }

        return AudioManager.GetClipFromAssetList("BGMusic1");
    } 

    public void SwitchBackgroundMusicUsingSwiftTransition()
    {
        AudioClip clip = GetNextBackgroundMusicClip();
        Debug.Log("Clip Name: " + clip.name);
        AudioManager.PlayBGM(clip, MusicTransition.Swift);
    }

    public void SwitchBackgroundMusicUsingFadeTransition()
    {
        AudioClip clip = GetNextBackgroundMusicClip();
        Debug.Log("Clip Name: " + clip.name);
        AudioManager.PlayBGM(clip, MusicTransition.FadeOutFadeIn);
    }

    public void SwitchBackgroundMusicUsingCrossfadeTransition()
    {
        AudioClip clip = GetNextBackgroundMusicClip();
        Debug.Log("Clip Name: " + clip.name);
        AudioManager.PlayBGM(clip, MusicTransition.CrossFade);
    }

    public void PlayOneShotSoundEffect()
    {
        AudioManager.PlayOneShot(AudioManager.GetClipFromAssetList("OneShot"));
    }

    public void PlayRepeatSoundEffect(int amount)
    {
        repeatAmount = amount;
        repeatSource = AudioManager.PlaySFX(AudioManager.GetClipFromAssetList("TickTock"), amount);
        totalTime = repeatSource.clip.length * amount;
    }

    public void PlayCallbackSoundEffect()
    {
        callbackSource = AudioManager.PlayOneShot(AudioManager.GetClipFromAssetList("Callback1"), CallbackFunction);
    }

    public void CallbackFunction()
    {
        AudioManager.PlayOneShot(AudioManager.GetClipFromAssetList("Callback2"));
    }
}
