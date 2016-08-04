using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;


[RequireComponent(typeof(AudioController))]
public class AudioManager : MonoBehaviour
{
    // singleton design pattern
    public static AudioManager Instance
    {
        get
        {
            if (!alive)
            {
                return null;
            }

            // check if there is a saved instance
            if (instance == null)
            {
                // find from the list or hierrachy
                instance = GameObject.FindObjectOfType<AudioManager>();

                // if none exists in scene
                if (instance == null)
                {
                    // create new one
                    lock (gameObjectLock)
                    {
                        gameObjectLock = new GameObject("AudioManager");
                        gameObjectLock.hideFlags = HideFlags.NotEditable;
                        // add the component to the gameobject
                        gameObjectLock.AddComponent<AudioManager>();
                    }
                }
            }

            return instance;
        }
    }

    private static AudioManager instance;
    private static GameObject gameObjectLock;
    private static bool alive = true;

    void OnApplicationExit()
    {
        alive = false;
    }

    void Awake()
    {
        if (instance == null)
        {
            DontDestroyOnLoad(this.gameObject);
            instance = this;
            OnAwake();
        }
        else if (instance != this)
        {
            Destroy(this.gameObject);
        }
    }

    #region Inspector Variables
    [SerializeField]
    private AbstractBackgroundMusic backgroundMusic;
    public static AbstractBackgroundMusic BGM
    {
        get { return Instance.backgroundMusic; }
    }

    [SerializeField]
    private List<AbstractLoopingSoundAsset> loopingSounds = new List<AbstractLoopingSoundAsset>();
    public static List<AbstractLoopingSoundAsset> LoopingSounds
    {
        get { return Instance.loopingSounds; }
    }

    [SerializeField]
    private List<AudioAsset> soundAssets = new List<AudioAsset>();
    public static List<AudioAsset> SoundAssets
    {
        get { return Instance.soundAssets; }
    }
    #endregion

    public static AudioController Controller = null;
    private bool initialised = false;
    public static bool Initialised
    {
        get { return Instance.initialised; }
    }


    static AudioSource musicSource, crossfadeSource;
    static float musicVol, sfxVol, crossfadeVol;
    static float musicCap;
    static bool sfxOn;

    static readonly string BackgroundMusicVolKey = "BGMVol";
    static readonly string SoundEffectsVolKey = "SFXVol";
    static readonly string DefaultSFXTag = "SFX";

    void OnAwake()
    {
        Debug.Log("AudioManager On Awake");
        Controller = GetComponent<AudioController>();
        Initialise();
    }

    void OnDestroy()
    {
        StopAllCoroutines();
        SavePreferences();
    }

    void SetupMusicSource()
    {
        musicSource = GetComponent<AudioSource>();

        if (musicSource == null)
        {
            musicSource = this.gameObject.AddComponent<AudioSource>() as AudioSource;
        }

        // setup settings if one exists
        musicSource.outputAudioMixerGroup = (Controller.MasterMixer != null) ? Controller.MasterMixer.outputAudioMixerGroup : null;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0;
        musicSource.rolloffMode = AudioRolloffMode.Logarithmic;
        // we set the loop setting to true, the music will loop forever
        musicSource.loop = true;
        // mute the sound if the if the music's been turned off
        musicSource.mute = !Controller.MusicOn;

        if (Controller.MasterMixer != null)
        {
            Controller.MasterMixer.GetFloat(BackgroundMusicVolKey, out musicVol);
            musicVol += 80f;
            musicVol /= 100f;
        }
        // set volume
        musicSource.volume = (Controller.MasterMixer != null) ? musicVol : Controller.MusicVolume;
    }

    void Initialise()
    {
        loopingSounds.Clear();
        soundsInLoop.Clear();
        LoadPreferences();
        SetupMusicSource();
        StartCoroutine(OnUpdate());
    }

    AudioSource AddMusicSource()
    {
        AudioSource audioSource = gameObject.AddComponent<AudioSource>() as AudioSource;

        audioSource.outputAudioMixerGroup = (Controller.MasterMixer != null) ? Controller.MasterMixer.outputAudioMixerGroup : null;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        // we set the loop setting to true, the music will loop forever
        audioSource.loop = true;
        // mute the sound if the if the music's been turned off
        audioSource.mute = !Controller.MusicOn;

        return audioSource;
    }

    IEnumerator OnUpdate()
    {
        while (true)
        {
            ManageLoopingSounds();

            // updates value if music volume or music mute has been changed
            if (musicVol != Controller.MusicVolume || Controller.MusicOn != !musicSource.mute)
            {
                musicVol = Controller.MusicVolume;
                musicSource.volume = musicVol;
                musicSource.mute = !Controller.MusicOn;

                if (Controller.MasterMixer != null)
                {
                    float mixerVol = -80f + (musicVol * 100f);
                    Controller.MasterMixer.SetFloat(BackgroundMusicVolKey, mixerVol);
                }
            }

            // updates value if sound effects volume or sound effects mute has been changed
            if (sfxVol != Controller.SoundFxVolume || Controller.SoundFxOn != sfxOn)
            {
                sfxVol = Controller.SoundFxVolume;
                AudioSource source;
                //foreach (GameObject g in GameObject.FindGameObjectsWithTag(DefaultSFXTag))
                foreach (SoundEFfectTag t in GameObject.FindObjectsOfType<SoundEFfectTag>())
                {
                    //source = g.GetComponent<AudioSource>();
                    source = t.GetComponent<AudioSource>();
                    source.volume = sfxVol;
                    source.mute = !Controller.SoundFxOn;
                }

                if (Controller.MasterMixer != null)
                {
                    float mixerVol = -80f + (sfxVol * 100f);
                    Controller.MasterMixer.SetFloat(SoundEffectsVolKey, mixerVol);
                }

                sfxOn = Controller.SoundFxOn;
            }

            // update the music transition for cross fade in queue
            if (crossfadeSource != null)
            {
                CrossFadeBackgroundMusic();

                yield return new WaitForSeconds(.20f);
            }
            else
            {
                // update the music transition for fade in and fade out queue
                if (backgroundMusic.NextClip != null)
                {
                    FadeOutFadeInBackgroundMusic();

                    yield return new WaitForSeconds(.20f);
                }
            }

            yield return new WaitForEndOfFrame();
        }
    }

    void FadeOutFadeInBackgroundMusic()
    {
        if (backgroundMusic.Transition == MusicTransition.FadeOutFadeIn)
        {
            if (musicSource.clip.name == backgroundMusic.NextClip.name)
            {
                Controller.MusicVolume += .05f;

                if (Controller.MusicVolume >= musicCap)
                {
                    SetBGMVolume(musicCap);
                    ChangeBackgroundMusic(backgroundMusic.NextClip, musicSource.time);
                }
            }
            else
            {
                Controller.MusicVolume -= .05f;

                if (Controller.MusicVolume <= 0.00f)
                {
                    Controller.MusicVolume = 0;
                    PlayMusic(ref musicSource, backgroundMusic.NextClip, 0);
                }
            }
        }
    }

    void CrossFadeBackgroundMusic()
    {
        if (backgroundMusic.Transition == MusicTransition.CrossFade)
        {
            if (musicSource.clip.name != backgroundMusic.NextClip.name)
            {
                Controller.MusicVolume -= .05f;

                crossfadeVol = Mathf.Clamp01(musicCap - musicVol);
                crossfadeSource.volume = crossfadeVol;
                crossfadeSource.mute = musicSource.mute;

                if (Controller.MusicVolume <= 0.00f)
                {
                    SetBGMVolume(musicCap);
                    ChangeBackgroundMusic(backgroundMusic.NextClip, crossfadeSource.time);
                }
            }
        }
    }

    void ManageLoopingSounds()
    {
        for (int i = 0; i < loopingSounds.Count; i++)
        {
            AbstractLoopingSoundAsset lpa = loopingSounds[i];
            lpa.Duration -= Time.deltaTime;
            loopingSounds[i] = lpa;

            if (loopingSounds[i].Duration <= 0)
            {
                Destroy(soundsInLoop[loopingSounds[i].Name]);
                soundsInLoop.Remove(loopingSounds[i].Name);
                LoopingSounds.RemoveAt(i);
                LoopingSounds.Sort();
                break;
            }
        }
    }

    #region Static Functions
    /// <summary>
    /// Changes the current audio clip of the background music.
    /// </summary>
    /// <param name="music_clip">Your audio clip.</param>
    /// <param name="playback_position">Play position of the clip.</param>
    static void ChangeBackgroundMusic(AudioClip music_clip, float playback_position)
    {
        // set the music source to play the current music
        PlayMusic(ref musicSource, music_clip, playback_position);
        // remove the call to next playing clip on queue
        Instance.backgroundMusic.NextClip = null;
        // we set the current playing clip
        Instance.backgroundMusic.CurrentClip = music_clip;
        // get rid of the crossfade source if there is one
        if (crossfadeSource != null)
        {
            Destroy(crossfadeSource);
            crossfadeSource = null;
        }
    }

    /// <summary>
    /// Plays a clip from the specified audio source.
    /// Creates and assigns an audio source component if the refrence is null.
    /// </summary>
    /// <param name="audio_source">Audio source / channel.</param>
    /// <param name="music_clip">Your audio clip.</param>
    /// <param name="playback_position">Play position of the clip.</param>
    static void PlayMusic(ref AudioSource audio_source, AudioClip music_clip, float playback_position)
    {
        if (audio_source == null)
        {
            Controller = Instance.GetComponent<AudioController>();
            audio_source = Instance.AddMusicSource();
        }

        audio_source.clip = music_clip;
        // we start playing the source clip at the destinated play back position
        //audio_source.PlayScheduled (playback_position);
        audio_source.time = playback_position;
        audio_source.Play();
    }

    /// <summary>
    /// Plays a background music using the swift the transition mode.
    /// Only one background music can be active at a time.
    /// </summary>
    /// <param name="music_clip">Your audio clip.</param>
    public static void PlayBGM(AudioClip music_clip)
    {
        PlayBGM(music_clip, MusicTransition.Swift);
    }

    /// <summary>
    /// Plays a background music.
    /// Only one background music can be active at a time.
    /// </summary>
    /// <param name="music_clip">Your audio clip.</param>
    /// <param name="transition_mode">Mode of music Transition.</param>
    public static void PlayBGM(AudioClip music_clip, MusicTransition transition_mode)
    {
        // if it's the first music to be played then switch over immediately - meaning no transition effect
        if (Instance.backgroundMusic.CurrentClip == null)
        {
            transition_mode = MusicTransition.Swift;
        }
        // stop if trying to play thesame music
        else if (Instance.backgroundMusic.CurrentClip == music_clip)
        {
            return;
        }

        // save the transition effect for the queue
        Instance.backgroundMusic.Transition = transition_mode;

        // start playing from the beginning if there is no effect mode
        if (Instance.backgroundMusic.Transition == MusicTransition.Swift)
        {
            ChangeBackgroundMusic(music_clip, 0);
        }
        else
        {
            // stop!!! has not finished fading the background music
            if (Instance.backgroundMusic.NextClip != null) return;

            musicCap = Controller.MusicVolume;
            Instance.backgroundMusic.NextClip = music_clip;

            if (Instance.backgroundMusic.Transition == MusicTransition.CrossFade)
            {
                // stop!!! has not finished crossfading the background music
                if (crossfadeSource != null) return;

                crossfadeSource = Instance.AddMusicSource();
                crossfadeSource.volume = crossfadeVol = Mathf.Clamp01(musicCap - musicVol);
                PlayMusic(ref crossfadeSource, Instance.backgroundMusic.NextClip, 0);
            }
        }
    }

    /// <summary>
    /// Plays a background music from the AudioManager Audio Asset List using the swift transition mode
    /// Only one background music can be active at a time.
    /// </summary>
    /// <param name="music_clip">Your audio clip.</param>
    public static void PlayBGMFromAsset(string name)
    {
        PlayBGMFromAsset(name, MusicTransition.Swift);
    }

    /// <summary>
    /// Plays a background music from the AudioManager Audio Asset List
    /// Only one background music can be active at a time.
    /// </summary>
    /// <param name="music_clip">Your audio clip.</param>
    /// <param name="transition_mode">Mode of music Transition.</param>
    public static void PlayBGMFromAsset(string name, MusicTransition transition_mode)
    {
        AudioClip clip = GetClipFromAsset(name);

        if (clip == null)
        {
            Debug.LogError("Could not find specified Clip[" + name + "] in AudioManager Audio Asset List.");
            return;
        }

        PlayBGM(clip, transition_mode);
    }

    /// <summary>
    /// Plays a background music from the Resources folder using the swift transition mode.
    /// Only one background music can be active at a time.
    /// </summary>
    /// <param name="name">Clip name from the Resources path directory.</param>
    public static void PlayBGMFromResource(string name)
    {
        PlayBGMFromResource(name, MusicTransition.Swift);
    }

    /// <summary>
    /// Plays a background music from a specified resource path.
    /// Only one background music can be active at a time.
    /// </summary>
    /// <param name="name">Clip name from the Resources path directory.</param>
    /// <param name="transition_mode">Mode of music Transition.</param>
    public static void PlayBGMFromResource(string name, MusicTransition transition_mode)
    {
        AudioClip musicCLip = Resources.Load(name) as AudioClip;
        if (musicCLip == null)
        {
            Debug.LogError(string.Format("AudioClip '{0}' not found at location {1}", name, Application.persistentDataPath));
        }

        PlayBGM(musicCLip, transition_mode);
    }

    /// <summary>
    /// Loads an AudioClip from the Resources folder
    /// </summary>
    /// <param name="name">Name of your audio clip.</param>
    public static AudioClip LoadClipFromResources(string name)
    {
        return Resources.Load(name) as AudioClip;
    }

    /// <summary>
    /// Stops the playing background music
    /// </summary>
    public static void StopBGM()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

    /// <summary>
    /// Inner function used to play all resulting sound effects.
    /// Sets-up some particular properties for the sound effect.
    /// </summary>
    /// <param name="sound_clip">Clip to play.</param>
    /// <param name="repeat">Loop or repeat the clip.</param>
    /// <param name="location">Location of the audio clip.</param>
    static GameObject SetupSoundEffect(AudioClip sound_clip, int repeat, Vector3 location)
    {
        // we create a temporary game object to host our audio source
        GameObject host = new GameObject("TempAudio");
        // we set the temp audio's position
        host.transform.position = location;
        // we specity a tag for future use
        //host.gameObject.tag = DefaultSFXTag;
        host.AddComponent<SoundEFfectTag>();
        // we add an audio source to that host
        AudioSource audioSource = host.AddComponent<AudioSource>() as AudioSource;
        audioSource.outputAudioMixerGroup = (Controller.MasterMixer != null) ? Controller.MasterMixer.outputAudioMixerGroup : null;
        // we set that audio source clip to the one in paramaters
        audioSource.clip = sound_clip;

        // we set the mute value
        audioSource.mute = !Controller.SoundFxOn;
        // we set whether to loop the sound
        audioSource.loop = (repeat > 1);


        // we set the audio source volume to the one in parameters
        audioSource.volume = Controller.SoundFxVolume;

        // we return the gameobject host reference
        return host;
    }

    /// <summary>
    /// Plays a sound effect and calls the specified callback function after the sound is over.
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">The sound clip you want to play.</param>
    /// <param name="repeat">How many times in successions you want the clip to play.</param>
    /// <param name="location">Location of the clip.</param>
    public static AudioSource PlaySFX(AudioClip clip, int repeat, Vector3 location)
    {
        return PlaySFX(clip, repeat, location, null);
    }


    /// <summary>
    /// Plays a sound effect and calls the specified callback function after the sound is over.
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">The sound clip you want to play.</param>
    /// <param name="repeat">How many times in successions you want the clip to play.</param>
    /// <param name="location">Location of the clip.</param>
    /// <param name="callback">Action callback to be invoked after the sound has finished.</param>
    public static AudioSource PlaySFX(AudioClip clip, int repeat, Vector3 location, Action callback)
    {
        GameObject host = SetupSoundEffect(clip, repeat, Vector3.zero);
        // get the Audiosource component attached to the host
        AudioSource source = host.GetComponent<AudioSource>();
        // we start playing the sound
        source.Play();
        // we destroy the host after the clip has played
        Destroy(host, clip.length * repeat);

        if (callback != null)
        {
            Instance.StartCoroutine(Instance.InvokeFunctionAfter(callback, clip.length * repeat));
        }

        return source;
    }

    /// <summary>
    /// Plays a sound effect from the Resources folder.
    /// </summary>
    /// <returns>An AudioSource</returns>
    /// <param name="name">Clip name from the ResourcePath.SoundFX directory.</param>
    /// <param name="repeat">How many times in successions you want the sound to play.</param>
    /// <param name="location">The location of the sound.</param>
    public static AudioSource PlaySFXFromResource(string name, int repeat, Vector3 location)
    {
        return PlaySFXFromResource(name, repeat, location, null);
    }

    /// <summary>
    /// Plays a sound effect from the Resources folder and calls the specified callback function after the sound is over.
    /// </summary>
    /// <returns>An AudioSource</returns>
    /// <param name="name">Clip name from the resource path directory.</param>
    /// <param name="repeat">How many times in successions you want the sound to play.</param>
    /// <param name="location">The location of the sound.</param>
    /// <param name="callback">Action callback to be invoked after playing sound.</param>
    public static AudioSource PlaySFXFromResource(string name, int repeat, Vector3 location, Action callback)
    {
        AudioClip clip = Resources.Load(name) as AudioClip;
        if (clip == null)
        {
            Debug.LogError(string.Format("AudioClip '{0}' not found at location {1}", name, Application.persistentDataPath));
            return null;
        }

        return PlaySFX(clip, repeat, location, callback);
    }

    /// <summary>
    /// Plays a sound effect once
    /// </summary>
    /// <returns>An AudioSource</returns>
    /// <param name="clip">The sound clip you want to play.</param>
    public static AudioSource PlayOneShot(AudioClip clip)
    {
        return PlayOneShot(clip, null);
    }

    /// <summary>
    /// Plays a sound effect once
    /// </summary>
    /// <returns>An AudioSource</returns>
    /// <param name="clip">The sound clip you want to play.</param>
    /// <param name="callback">Action callback to be invoked after clip has finished playing.</param>
    public static AudioSource PlayOneShot(AudioClip clip, Action callback)
    {
        GameObject host = SetupSoundEffect(clip, 1, Vector3.zero);

        AudioSource source = host.GetComponent<AudioSource>();
        source.spatialBlend = 0;
        source.rolloffMode = AudioRolloffMode.Logarithmic;

        // we start playing the sound
        source.Play();
        // we destroy the host after the clip has played
        Destroy(host, clip.length);

        if (callback != null)
        {
            Instance.StartCoroutine(Instance.InvokeFunctionAfter(callback, clip.length));
        }

        return source;
    }

    /// <summary>
    /// Plays a sound effect once from the Resources folder.
    /// </summary>
    /// <returns>An Audiosource</returns>
    /// <param name="name">Clip name from the ResourcePath.SoundFX directory.</param>
    public static AudioSource PlayOneShotFromResource(string name)
    {
        return PlayOneShotFromResource(name, null);
    }

    /// <summary>
    /// Plays a sound effect once from the specified resouce path.
    /// </summary>
    /// <returns>An Audiosource</returns>
    /// <param name="path">Resource path of the clip.</param>
    /// <param name="name">Clip name from the ResourcePath.SoundFX directory.</param>
    /// <param name="callback">Action callback to be invoked after clip has finished playing.</param>
    public static AudioSource PlayOneShotFromResource(string name, Action callback)
    {
        AudioClip clip = Resources.Load(name) as AudioClip;
        if (clip == null)
        {
            Debug.LogError(string.Format("AudioClip '{0}' not found at location {1}", name, Application.persistentDataPath));
            return null;
        }

        return PlayOneShot(clip, callback);
    }

    // Inner function used to callback a looping or repating clip if a callback function was passed as a parameter
    IEnumerator InvokeFunctionAfter(Action callback, float time)
    {
        yield return new WaitForSeconds(time);

        callback.Invoke();
    }

    /// <summary>
    /// Toggles the Background Music mute.
    /// </summary>
    public static void ToggleBGMMute()
    {
        Controller.MusicOn = !Controller.MusicOn;
        musicSource.mute = !Controller.MusicOn;
    }

    /// <summary>
    /// Toggles the Sound Effect mute.
    /// </summary>
    public static void ToggleSFXMute()
    {
        Controller.SoundFxOn = !Controller.SoundFxOn;

        AudioSource source;
        //foreach (GameObject g in GameObject.FindGameObjectsWithTag(DefaultSFXTag))
        foreach (SoundEFfectTag t in GameObject.FindObjectsOfType<SoundEFfectTag>())
        {
            //source = g.GetComponent<AudioSource>();
            source = t.GetComponent<AudioSource>();
            source.volume = sfxVol;
            source.mute = !Controller.SoundFxOn;
        }

        sfxOn = Controller.SoundFxOn;
    }

    /// <summary>
    /// Toggles the Mater Volume that controls both Background Music & Sound Effect mute.
    /// </summary>
    public static void ToggleMute()
    {
        ToggleBGMMute();
        ToggleSFXMute();
    }

    /// <summary>
    /// Sets and saves the background music volume.
    /// </summary>
    /// <param name="volume">New volume of the background music.</param>
    public static void SetBGMVolume(float volume)
    {
        if (musicSource == null)
        {
            Controller = Instance.GetComponent<AudioController>();
            musicSource = Instance.AddMusicSource();
        }

        volume = Mathf.Clamp01(volume);
        musicVol = Controller.MusicVolume = volume;
        musicSource.volume = musicVol;
        SaveBGMVolume();

        if (Controller.MasterMixer != null)
        {
            // Always [-80db ... 20db]
            float mixerVol = -80f + (volume * 100f);
            Controller.MasterMixer.SetFloat(BackgroundMusicVolKey, mixerVol);
        }
    }

    /// <summary>
    /// Sets and saves the sound effect volume.
    /// </summary>
    /// <param name="volume">New volume of all sound effects.</param>
    public static void SetSFxVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        sfxVol = Controller.SoundFxVolume = volume;
        AudioSource source;
        //foreach (GameObject g in GameObject.FindGameObjectsWithTag(DefaultSFXTag))
        foreach (SoundEFfectTag t in GameObject.FindObjectsOfType<SoundEFfectTag>())
        {
            //source = g.GetComponent<AudioSource>();
            source = t.GetComponent<AudioSource>();
            source.volume = sfxVol;
        }
        SaveSFXVolume();

        if (Controller.MasterMixer != null)
        {
            // Always [-80db ... 20db]
            float mixerVol = -80f + (volume * 100f);
            Controller.MasterMixer.SetFloat(SoundEffectsVolKey, mixerVol);
        }
    }

    // Self explanatory
    static void SaveSFXVolume()
    {
        PlayerPrefs.SetFloat(SoundEffectsVolKey, Controller.SoundFxVolume);
        PlayerPrefs.Save();
    }

    // Self explanatory
    static void SaveBGMVolume()
    {
        PlayerPrefs.SetFloat(BackgroundMusicVolKey, Controller.MusicVolume);
        PlayerPrefs.Save();
    }

    // Self explanatory
    static float LoadBGMVolume()
    {
        return (PlayerPrefs.HasKey(BackgroundMusicVolKey)) ? PlayerPrefs.GetFloat(BackgroundMusicVolKey) : AudioController.DefaultMusicVol;
    }

    // Self explanatory
    static float LoadSFxVolume()
    {
        return (PlayerPrefs.HasKey(SoundEffectsVolKey)) ? PlayerPrefs.GetFloat(SoundEffectsVolKey) : AudioController.DefaultSFxVol;
    }

    // Self explanatory
    public static void ClearPreferences()
    {
        PlayerPrefs.DeleteKey(BackgroundMusicVolKey);
        PlayerPrefs.DeleteKey(SoundEffectsVolKey);
        PlayerPrefs.Save();
    }

    // Self explanatory
    public static void LoadPreferences()
    {
        musicVol = Controller.MusicVolume = LoadBGMVolume();
        sfxVol = Controller.SoundFxVolume = LoadSFxVolume();
        sfxOn = Controller.SoundFxOn;
    }

    // Self explanatory
    public static void SavePreferences()
    {
        PlayerPrefs.SetFloat(SoundEffectsVolKey, Controller.SoundFxVolume);
        PlayerPrefs.SetFloat(BackgroundMusicVolKey, Controller.MusicVolume);
        PlayerPrefs.Save();
    }

    Dictionary<string, GameObject> soundsInLoop = new Dictionary<string, GameObject>();
    // Looping a particular list of sounds for a particular time 
    // Used for powerups that have timers that can be reset when another of the same type is collected
    public static void LoopSound(AudioClip clip, Vector3 location, float duration)
    {
        AbstractLoopingSoundAsset loopSound = new AbstractLoopingSoundAsset("", null, 0);
        loopSound.Name = clip.name;
        loopSound.Clip = clip;
        loopSound.Duration = duration;

        if (Instance.soundsInLoop.ContainsKey(clip.name))
        {
            // simply reset the duration if it exists
            int index = GetLoopingSoundIndex(clip.name);
            Instance.loopingSounds[index] = loopSound;
        }
        else
        {
            // create a new looping sound and add it to the library
            LoopingSounds.Add(loopSound);

            int repeat = (int)(duration / clip.length);
            GameObject host = SetupSoundEffect(clip, repeat, Vector3.zero);
            // get the Audiosource component attached to the host
            AudioSource source = host.GetComponent<AudioSource>();
            // we start playing the sound
            source.Play();

            Instance.soundsInLoop.Add(loopSound.Name, host);
        }
    }

    static int GetLoopingSoundIndex(string name)
    {
        int index = 0;
        while (index < LoopingSounds.Count)
        {
            if (LoopingSounds[index].Name == name)
            {
                return index;
            }

            index++;
        }

        return -1;
    }


    public static void EmptyAssetList()
    {
        SoundAssets.Clear();
    }

    public static void AddToAssetList(string path, string name)
    {
        AudioClip clip = Resources.Load<AudioClip>(path + name);

        AudioAsset sndAsset;
        sndAsset.Name = clip.name;
        sndAsset.Clip = clip;

        SoundAssets.Add(sndAsset);
    }

    public static void LoadSoundsIntoAssets(string path)
    {
        AudioClip[] clips = Resources.LoadAll<AudioClip>(path);

        SoundAssets.Clear();

        AudioAsset sndAsset;

        for (int i = 0; i < clips.Length; i++)
        {
            sndAsset.Name = clips[i].name;
            sndAsset.Clip = clips[i];
            SoundAssets.Add(sndAsset);
        }
    }

    public static AudioClip GetClipFromAsset(string clip_name)
    {
        foreach (AudioAsset sndAsset in SoundAssets)
        {
            if (clip_name == sndAsset.Name)
            {
                return sndAsset.Clip;
            }
        }

        return null;
    }
    #endregion
}
