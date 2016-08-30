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
            // check if application is shutting down and audio manager is destroyed
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

    // singleton placeholder
    private static AudioManager instance;
    private static GameObject gameObjectLock;
    // application has started and is running
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
    private BackgroundMusic backgroundMusic;
    [SerializeField]
    private List<RepeatSound> repeatingSounds = new List<RepeatSound>();
    [SerializeField]
    private List<AudioAsset> audioAssets = new List<AudioAsset>();
    #endregion

    #region Static Variables
    public static AudioController Controller
    {
        get { return Instance.audioController; }
    }

    public static BackgroundMusic BGM
    {
        get { return Instance.backgroundMusic; }
    }

    public static List<RepeatSound> RepeatSoundPool
    {
        get { return Instance.repeatingSounds; }
    }

    public static List<AudioAsset> AudioAssetPool
    {
        get { return Instance.audioAssets; }
    }
   
    
    static AudioSource musicSource = null, crossfadeSource = null;
    static float musicVol = 0, sfxVol = 0, crossfadeVol = 0, musicVolCap = 0;
    static bool sfxOn = false, isActive = false;

    static readonly string BackgroundMusicVolKey = "BGMVol";
    static readonly string SoundEffectsVolKey = "SFXVol";
    static readonly string BackgroundMusicStatusKey = "BGMStatus";
    static readonly string SoundEffectsStatusKey = "SFXStatus";
    static readonly string DefaultSFXTag = "SFX";

    AudioController audioController = null;
    #endregion

    void OnDestroy()
    {
        Debug.Log("OnDestroy");
        isActive = false;
        StopAllCoroutines();
        SavePreferences();
    }

    // initialise the audio manager
    void OnAwake()
    {
        Debug.Log("AudioManager On Awake");
        audioController = GetComponent<AudioController>();
        repeatingSounds.Clear();
        SetupMusicAudioSource();
    }

    // initialises the audio source used by the background music
    void SetupMusicAudioSource()
    {
        // find audio source component attached to audio manager
        musicSource = GetComponent<AudioSource>();

        // if none exists, create one and attach to audiomanager
        if (musicSource == null)
        {
            musicSource = this.gameObject.AddComponent<AudioSource>() as AudioSource;
        }

        // set the default settings or properties to be used by the audio source
        musicSource.outputAudioMixerGroup = Controller.MusicMixerGroup;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0;
        musicSource.rolloffMode = AudioRolloffMode.Logarithmic;
        musicSource.loop = true;

        // is the background music source using a master mixer
        /*
        if (musicSource.outputAudioMixerGroup != null)
        {
            Debug.Log(musicSource.outputAudioMixerGroup.name);
            //Debug.Log(musicSource.outputAudioMixerGroup.audioMixer.name);

            AudioMixerGroup[] groups = musicSource.outputAudioMixerGroup.audioMixer.FindMatchingGroups("Music");

            for (int i = 0; i < groups.Length; i++)
            {
                Debug.Log(">>");
                Debug.Log(groups[i].name);
            }
        }
        */
    }

    void Start()
    {
        if (Controller != null && musicSource != null && !isActive)
        {
            OnStart();
        }
    }

    // this is here because the mixer group float can't be set awake
    // spent amost 4 hours trying to figure out why... well didn't know until i ran some tests 
    void OnStart()
    {
        audioController.MusicOn = LoadBGMStatus();
        musicSource.mute = !audioController.MusicOn;
        SetBGMVolume(LoadBGMVolume());

        sfxOn = audioController.SoundFxOn = LoadSFxStatus();
        SetSFxVolume(LoadSFxVolume());

        SavePreferences();

        if (!isActive)
        {
            StartCoroutine(OnUpdate());
        }

        isActive = true;
    }

    AudioSource AttachAudioSource()
    {
        AudioSource audioSource = gameObject.AddComponent<AudioSource>() as AudioSource;

        audioSource.outputAudioMixerGroup = Controller.MusicMixerGroup;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        // we set the loop setting to true to loop the clip forever
        audioSource.loop = true;
        // we set the volume level of the audio source
        audioSource.volume = LoadBGMVolume();
        // we set the mute settings to the controller's settings
        audioSource.mute = !audioController.MusicOn;

        return audioSource;
    }

    void ManageRepeatingSounds()
    {
        for (int i = 0; i < repeatingSounds.Count; i++)
        {
            RepeatSound rs = repeatingSounds[i];
            rs.Duration -= Time.deltaTime;
            repeatingSounds[i] = rs;

            if (repeatingSounds[i].Duration <= 0)
            {
                if (repeatingSounds[i].Callback != null)
                {
                    repeatingSounds[i].Callback.Invoke();
                }

                // destroy the host after
                Destroy(repeatingSounds[i].Source.gameObject);

                repeatingSounds.RemoveAt(i);
                repeatingSounds.Sort();
                break;
            }
        }
    }

    // has the music volume or the music mute status been changed
    bool IsMusicAltered()
    {
        bool flag = audioController.MusicOn != !musicSource.mute || musicVol != audioController.MusicVolume;

        if (audioController.MusicMixerGroup != null)
        {
            float vol;
            // get the music volume from the master mixer 
            audioController.MusicMixerGroup.audioMixer.GetFloat(BackgroundMusicVolKey, out vol);
            // make it a range from 0 to 1 to suit the music source volume and audiomanager volume
            vol += 80f;
            vol /= 100f;

            return flag || musicVol != vol;
        }

        return flag;
    }

    // has the music volume or the music mute status been changed
    bool IsSoundFxAltered()
    {
        bool flag = audioController.SoundFxOn != sfxOn || sfxVol != audioController.SoundFxVolume;

        if (audioController.SoundFxMixerGroup != null)
        {
            float vol;
            // get the music volume from the master mixer 
            audioController.SoundFxMixerGroup.audioMixer.GetFloat(SoundEffectsVolKey, out vol);
            // make it a range from 0 to 1 to suit the music source volume and audiomanager volume
            vol += 80f;
            vol /= 100f;

            return flag || sfxVol != vol;
        }

        return flag;
    }

    void CrossFadeBackgroundMusic()
    {
        if (backgroundMusic.Transition == MusicTransition.CrossFade)
        {
            if (musicSource.clip.name != backgroundMusic.NextClip.name)
            {
                audioController.MusicVolume -= .05f;

                crossfadeVol = Mathf.Clamp01(musicVolCap - musicVol);
                crossfadeSource.volume = crossfadeVol;
                crossfadeSource.mute = musicSource.mute;

                if (audioController.MusicVolume <= 0.00f)
                {
                    SetBGMVolume(musicVolCap);
                    ChangeBackgroundMusic(backgroundMusic.NextClip, crossfadeSource.time);
                }
            }
        }
    }

    void FadeOutFadeInBackgroundMusic()
    {
        if (backgroundMusic.Transition == MusicTransition.FadeOutFadeIn)
        {
            if (musicSource.clip.name == backgroundMusic.NextClip.name)
            {
                audioController.MusicVolume += .05f;

                if (audioController.MusicVolume >= musicVolCap)
                {
                    SetBGMVolume(musicVolCap);
                    ChangeBackgroundMusic(backgroundMusic.NextClip, musicSource.time);
                }
            }
            else
            {
                audioController.MusicVolume -= .05f;

                if (audioController.MusicVolume <= 0.00f)
                {
                    audioController.MusicVolume = 0;
                    PlayMusic(ref musicSource, backgroundMusic.NextClip, 0);
                }
            }
        }
    }

    IEnumerator OnUpdate()
    {
        while (true)
        {
            ManageRepeatingSounds();

            if (isActive)
            {
                // updates value if music volume or music mute status has been changed
                if (IsMusicAltered())
                {
                    musicSource.mute = !audioController.MusicOn;

                    if (musicVol != audioController.MusicVolume)
                    {
                        musicVol = audioController.MusicVolume;
                    }
                    else if (audioController.MusicMixerGroup != null)
                    {
                        float vol;
                        audioController.MusicMixerGroup.audioMixer.GetFloat(BackgroundMusicVolKey, out vol);
                        vol += 80f;
                        vol /= 100f;
                        musicVol = vol;
                    }

                    SetBGMVolume(musicVol);
                }

                // updates value if sound effects volume or sound effects mute has been changed
                if (IsSoundFxAltered())
                {
                    sfxOn = audioController.SoundFxOn;

                    if (sfxVol != audioController.SoundFxVolume)
                    {
                        sfxVol = audioController.SoundFxVolume;
                    }
                    else if (audioController.MusicMixerGroup != null)
                    {
                        float vol;
                        audioController.SoundFxMixerGroup.audioMixer.GetFloat(SoundEffectsVolKey, out vol);
                        vol += 80f;
                        vol /= 100f;
                        sfxVol = vol;
                    }

                    SetSFxVolume(sfxVol);
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
            }

            yield return new WaitForEndOfFrame();
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
            Instance.audioController = Instance.GetComponent<AudioController>();
            audio_source = Instance.AttachAudioSource();
            Instance.OnStart();
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

            musicVolCap = Controller.MusicVolume;
            Instance.backgroundMusic.NextClip = music_clip;

            if (Instance.backgroundMusic.Transition == MusicTransition.CrossFade)
            {
                // stop!!! has not finished crossfading the background music
                if (crossfadeSource != null) return;

                crossfadeSource = Instance.AttachAudioSource();
                crossfadeSource.outputAudioMixerGroup = null;
                crossfadeSource.volume = crossfadeVol = Mathf.Clamp01(musicVolCap - musicVol);
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
        AudioClip clip = GetClipFromAssetList(name);

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
        AudioClip musicClip = Resources.Load(name) as AudioClip;
        if (musicClip == null)
        {
            Debug.LogError(string.Format("AudioClip '{0}' not found at location {1}", name, System.IO.Path.Combine(Application.dataPath, "/Resources/")));
        }

        PlayBGM(musicClip, transition_mode);
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
    /// Pauses the playing background music
    /// </summary>
    public static void PauseBGM()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    /// <summary>
    /// Resumes the playing background music
    /// </summary>
    public static void ResumeBGM()
    {
        if (!musicSource.isPlaying)
        {
            musicSource.UnPause();
        }
    }


    /// <summary>
    /// Inner function used to play all resulting sound effects.
    /// Sets-up some particular properties for the sound effect.
    /// </summary>
    /// <param name="sound_clip">Clip to play.</param>
    /// <param name="repeat">Loop or repeat the clip.</param>
    /// <param name="location">Location of the audio clip.</param>
    static GameObject SetupSoundEffect(AudioClip sound_clip, bool loop, Vector2 location)
    {
        // create a temporary game object to host our audio source
        GameObject host = new GameObject("TempAudio");
        
        // set the temp audio's position
        host.transform.position = location;

        // specity a tag for future use
        //host.gameObject.tag = DefaultSFXTag;
        host.AddComponent<SoundEFfectTag>();

        // add an audio source to that host
        AudioSource audioSource = host.AddComponent<AudioSource>() as AudioSource;

        audioSource.outputAudioMixerGroup = Controller.SoundFxMixerGroup;
        // set that audio source clip to the one in paramaters
        audioSource.clip = sound_clip;
        // set the mute value
        audioSource.mute = !Controller.SoundFxOn;
        // set whether to loop the sound
        audioSource.loop = loop;
        // set the audio source volume to the one in parameters
        audioSource.volume = Controller.SoundFxVolume;

        return host;
    }


    /// <summary>
    /// Returns the index of a repeating sound in pool if one exists.
    /// </summary>
    /// <returns>Index of repeating sound or -1 is none exists</returns>
    /// <param name="name">The name of the repeating sound.</param>
    public static int GetRepeatingSoundIndex(string name)
    {
        int index = 0;
        while (index < Instance.repeatingSounds.Count)
        {
            if (Instance.repeatingSounds[index].Name == name)
            {
                return index;
            }

            index++;
        }

        return -1;
    }


    /// <summary>
    /// Plays a sound effect for a duration of time and calls the specified callback function after the time is over.
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">The sound clip you want to play.</param>
    /// <param name="duration">The length in time the clip should play.</param>
    /// <param name="location">Location of the clip.</param>
    /// <param name="callback">Action callback to be invoked after the sound has finished.</param>
    public static AudioSource PlaySFX(AudioClip clip, float duration, Vector2 location, Action callback)
    {
        if (GetRepeatingSoundIndex(clip.name) >= 0)
        {
            // simply reset the duration if it exists
            int index = GetRepeatingSoundIndex(clip.name);
            RepeatSound rs = Instance.repeatingSounds[index];
            rs.Duration = duration;
            Instance.repeatingSounds[index] = rs;
            return Instance.repeatingSounds[index].Source;
        }

        if (duration <= clip.length)
        {
            return PlayOneShot(clip, callback);
        }

        GameObject host = SetupSoundEffect(clip, duration > clip.length, Vector2.zero);
        
        // get the Audiosource component attached to the host
        AudioSource source = host.GetComponent<AudioSource>();
        // create a new repeat sound
        RepeatSound repeatSound;
        repeatSound.Name = clip.name;
        repeatSound.Source = source;
        repeatSound.Duration = duration;
        repeatSound.Callback = callback;
        // add it to the list
        Instance.repeatingSounds.Add(repeatSound);
        
        // start playing the sound
        source.Play();

        return source;
    }

    /// <summary>
    /// Plays a sound effect for a duration of time.
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">The sound clip you want to play.</param>
    /// <param name="duration">The length in time the clip should play.</param>
    /// <param name="location">Location of the clip.</param>
    public static AudioSource PlaySFX(AudioClip clip, float duration, Vector2 location)
    {
        return PlaySFX(clip, duration, location, null);
    }


    /// <summary>
    /// Plays a sound effect for a duration of time.
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">The sound clip you want to play.</param>
    /// <param name="duration">The length in time the clip should play.</param>
    public static AudioSource PlaySFX(AudioClip clip, float duration)
    {
        return PlaySFX(clip, duration, Vector2.zero, null);
    }


    /// <summary>
    /// Plays a sound effect and calls the specified callback function after the sound is over.
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">The sound clip you want to play.</param>
    /// <param name="repeat">How many times in successions you want the clip to play.</param>
    /// <param name="location">Location of the clip.</param>
    /// <param name="callback">Action callback to be invoked after the sound has finished.</param>
    public static AudioSource PlaySFX(AudioClip clip, int repeat, Vector2 location, Action callback)
    {
        return PlaySFX(clip, clip.length * repeat, location, callback);
    }


    /// <summary>
    /// Plays a sound effect and calls the specified callback function after the sound is over.
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">The sound clip you want to play.</param>
    /// <param name="repeat">How many times in successions you want the clip to play.</param>
    /// <param name="location">Location of the clip.</param>
    public static AudioSource PlaySFX(AudioClip clip, int repeat, Vector2 location)
    {
        return PlaySFX(clip, clip.length * repeat, location, null);
    }


    /// <summary>
    /// Plays a sound effect and calls the specified callback function after the sound is over.
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">The sound clip you want to play.</param>
    /// <param name="repeat">How many times in successions you want the clip to play.</param>
    public static AudioSource PlaySFX(AudioClip clip, int repeat)
    {
        return PlaySFX(clip, clip.length * repeat, Vector2.zero, null);
    }


    /// <summary>
    /// Pauses all the sound effects in the game
    /// </summary>
    public static void PauseAllSFX()
    {
        AudioSource source;
        foreach (SoundEFfectTag t in FindObjectsOfType<SoundEFfectTag>())
        {
            source = t.GetComponent<AudioSource>();
            if(source.isPlaying) source.Pause();
        }
    }

    /// <summary>
    /// Resumes all the sound effect in the game
    /// </summary>
    /// <param name="volume">New volume of all sound effects.</param>
    public static void ResumeAllSFX()
    {
        AudioSource source;
        foreach (SoundEFfectTag t in FindObjectsOfType<SoundEFfectTag>())
        {
            source = t.GetComponent<AudioSource>();
            if(!source.isPlaying) source.UnPause();
        }
    }


    // Inner function used to callback a looping or repating clip if a callback function was passed as a parameter
    IEnumerator InvokeFunctionAfter(Action callback, float time)
    {
        yield return new WaitForSeconds(time);

        callback.Invoke();
    }


    /// <summary>
    /// Plays a sound effect once
    /// </summary>
    /// <returns>An AudioSource</returns>
    /// <param name="clip">The sound clip you want to play.</param>
    /// <param name="callback">Action callback to be invoked after clip has finished playing.</param>
    public static AudioSource PlayOneShot(AudioClip clip, Action callback)
    {
        GameObject host = SetupSoundEffect(clip, false, Vector2.zero);

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
    /// Plays a sound effect once
    /// </summary>
    /// <returns>An AudioSource</returns>
    /// <param name="clip">The sound clip you want to play.</param>
    public static AudioSource PlayOneShot(AudioClip clip)
    {
        return PlayOneShot(clip, null);
    }

    /// <summary>
    /// Loads an AudioClip from the Resources folder
    /// </summary>
    /// <param name="name">Name of your audio clip.</param>
    public static AudioClip LoadClipFromResource(string name)
    {
        AudioClip clip = Resources.Load(name) as AudioClip;
        if (clip == null)
        {
            Debug.LogException(new UnityException(string.Format("AudioClip '{0}' not found at location {1}", 
                                                name, System.IO.Path.Combine(Application.dataPath, "/Resources/"))));
            return null;
        }

        return clip;
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
            Debug.LogError(string.Format("AudioClip '{0}' not found at location {1}", name, System.IO.Path.Combine(Application.dataPath, "/Resources/")));
            return null;
        }

        return PlayOneShot(clip, callback);
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
    /// Toggles the Background Music mute.
    /// </summary>
    public static void ToggleBGMMute()
    {
        Instance.audioController.MusicOn = !Controller.MusicOn;
        musicSource.mute = !Controller.MusicOn;
    }

    /// <summary>
    /// Toggles the Sound Effect mute.
    /// </summary>
    public static void ToggleSFXMute()
    {
        Instance.audioController.SoundFxOn = !Controller.SoundFxOn;

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
        if (Controller == null) return;

        //if (musicSource == null)
        //{
            //Instance.audioController = Instance.GetComponent<AudioController>();
            //musicSource = Instance.GetComponent<AudioSource>() == null ? Instance.AttachAudioSource() : Instance.GetComponent<AudioSource>();
            //Instance.OnStart();
        //}

        // make it a range from 0 to 1 to suit the music source volume and audiomanager volume
        volume = Mathf.Clamp01(volume);
        // assign vol to all music volume variables
        musicSource.volume = musicVol = Controller.MusicVolume = volume;
        // is the controller using a master mixer
        if (Controller.MusicMixerGroup != null)
        {
            // get the equivalent mixer volume, always [-80db ... 20db]
            float mixerVol = -80f + (volume * 100f);
            // set the volume of the background music group
            Controller.MusicMixerGroup.audioMixer.SetFloat(BackgroundMusicVolKey, mixerVol);
        }

        SaveBGMPreferences();
    }

    /// <summary>
    /// Sets and saves the sound effect volume.
    /// </summary>
    /// <param name="volume">New volume of all sound effects.</param>
    public static void SetSFxVolume(float volume)
    {
        if (Controller == null) return;

        volume = Mathf.Clamp01(volume);
        sfxVol = Controller.SoundFxVolume = volume;

        AudioSource source;
        foreach (SoundEFfectTag t in FindObjectsOfType<SoundEFfectTag>())
        {
            source = t.GetComponent<AudioSource>();
            source.volume = sfxVol;
            source.mute = !Controller.SoundFxOn;
        }
        
        // is the controller using a master mixer
        if (Controller.SoundFxMixerGroup != null)
        {
            // get the equivalent mixer volume, always [-80db ... 20db]
            float mixerVol = -80f + (volume * 100f);
            // set the volume of the sound effect group
            Controller.SoundFxMixerGroup.audioMixer.SetFloat(SoundEffectsVolKey, mixerVol);
        }

        SaveSFXPreferences();
    }

    // Self explanatory
    static void SaveSFXPreferences()
    {
        PlayerPrefs.SetInt(SoundEffectsStatusKey, Controller.SoundFxOn ? 1 : 0);
        PlayerPrefs.SetFloat(SoundEffectsVolKey, Controller.SoundFxVolume);
        PlayerPrefs.Save();
    }

    // Self explanatory
    static void SaveBGMPreferences()
    {
        PlayerPrefs.SetInt(BackgroundMusicStatusKey, Controller.MusicOn ? 1 : 0);
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

    static bool ToBool(int integer)
    {
        return integer == 0 ? false : true;
    }

    // Self explanatory
    static bool LoadBGMStatus()
    {
        return (PlayerPrefs.HasKey(BackgroundMusicStatusKey)) ? ToBool(PlayerPrefs.GetInt(BackgroundMusicStatusKey)) : Controller.MusicOn;
    }

    // Self explanatory
    static bool LoadSFxStatus()
    {
        return (PlayerPrefs.HasKey(SoundEffectsStatusKey)) ? ToBool(PlayerPrefs.GetInt(SoundEffectsStatusKey)) : Controller.SoundFxOn;
    }

    // Self explanatory
    public static void ClearPreferences()
    {
        PlayerPrefs.DeleteKey(BackgroundMusicVolKey);
        PlayerPrefs.DeleteKey(SoundEffectsVolKey);
        PlayerPrefs.DeleteKey(BackgroundMusicStatusKey);
        PlayerPrefs.DeleteKey(SoundEffectsStatusKey);
        PlayerPrefs.Save();
    }

    // Self explanatory
    public static void SavePreferences()
    {
        PlayerPrefs.SetFloat(SoundEffectsVolKey, Controller.SoundFxVolume);
        PlayerPrefs.SetFloat(BackgroundMusicVolKey, Controller.MusicVolume);
        PlayerPrefs.SetInt(SoundEffectsStatusKey, Controller.SoundFxOn ? 1 : 0);
        PlayerPrefs.SetInt(BackgroundMusicStatusKey, Controller.MusicOn ? 1 : 0);
        PlayerPrefs.Save();
    }


    public static void EmptyAssetList()
    {
        AudioAssetPool.Clear();
    }

    public static void AddToAssetList(string audio_path)
    {
        AudioClip clip = Resources.Load<AudioClip>(audio_path);

        if (clip == null)
        {
            Debug.LogError("Could not find specified Clip at path '" + audio_path + "'");
            return;
        }

        AudioAsset sndAsset;
        sndAsset.Name = clip.name;
        sndAsset.Clip = clip;

        AudioAssetPool.Add(sndAsset);
    }

    public static void LoadSoundsIntoAssetList(string audio_path)
    {
        AudioClip[] clips = Resources.LoadAll<AudioClip>(audio_path);

        AudioAssetPool.Clear();

        AudioAsset sndAsset;

        for (int i = 0; i < clips.Length; i++)
        {
            sndAsset.Name = clips[i].name;
            sndAsset.Clip = clips[i];
            AudioAssetPool.Add(sndAsset);
        }
    }

    public static AudioClip GetClipFromAssetList(string clip_name)
    {
        foreach (AudioAsset sndAsset in AudioAssetPool)
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
