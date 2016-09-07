using UnityEngine;
using UnityEngine.SceneManagement;


public class SceneOptions : MonoBehaviour
{
    // reference to the black screen animator component.
    public Animator blackscreenAnimator;
    // fade to transparent animation clip
    public AnimationClip fadeToTransparentAnimationClip;

    public bool IsInMainMenu
    {
        get { return SceneManager.GetActiveScene().name == "MenuScene"; }
    }

    // reference to the PanelOptions script
    PanelOptions panelOptions;                  

    
    void Awake()
	{
        // retrieve the attached PanelOptions script
        panelOptions = GetComponent<PanelOptions> ();
	}

    void Start()
	{
        // fade the black screen to transparent
        blackscreenAnimator.SetTrigger("toTransparent");
        // play right background music if none is currently active
		PlayMusic ();
	}

    public void PlayMusic()
    {
        AudioClip clip = null;

        // load the scenes by name to decide which music clip to play.
        switch (SceneManager.GetActiveScene().name)
        {
            // load the title music from the resources folder using AudioManager
            case "MenuScene":
                clip = AudioManager.LoadClipFromResource("MenuMusic");
                break;
            // load the game music from the resources folder using AudioManager
            case "GameScene":
                clip = AudioManager.LoadClipFromResource("GameMusic");
                break;
        }

        // play the background music using the fade transition from the assigned clip
        if (clip)
        {
            AudioManager.PlayBGM(clip, MusicTransition.FadeOutFadeIn);
        }
    }

    // function used to load or exit gameplay
    public void StartGame(bool flag)
    {
        if (flag)
            AudioManager.PlayOneShotFromResource("Button", LoadGame);
        else
            AudioManager.PlayOneShotFromResource("Button", LoadMainMenu);
    }

    void LoadGame()
    {
        // fade out current music and fade in next music in 1s
        AudioManager.PlayBGMFromResource("GameMusic", MusicTransition.FadeOutFadeIn);
        
        // disable interaction with the main menu UI
        panelOptions.DisableMainMenu();

        // delay calling of LoadGameScene by half the length of fadeColorAnimationClip
        Invoke("LoadGameScene", fadeToTransparentAnimationClip.length * 1);

        // trigger the transparent Animator to start transition to the FadeToOpaque state.
        blackscreenAnimator.SetTrigger("toOpaque");
    }

    void LoadGameScene()
	{
		SceneManager.LoadScene("GameScene");
	}

    void LoadMainMenu()
    {
        // fade out current music and fade in next music in 1s
        AudioManager.PlayBGMFromResource("MenuMusic", MusicTransition.FadeOutFadeIn);

        // disable interaction with the pause menu UI
        panelOptions.DisablePauseMenu();

        // delay calling of LoadMenuScene by half the length of fadeColorAnimationClip
        Invoke("LoadMenuScene", fadeToTransparentAnimationClip.length * 1);

        // trigger of Animator 'animColorFade' to start transition to the FadeToOpaque state.
        blackscreenAnimator.SetTrigger("toOpaque");
    }

	void LoadMenuScene()
	{
        SceneManager.LoadScene("MenuScene");
    }

    // display on or off the options menu after the button sound has been played
    public void DisplayOptionsMenu(bool flag)
    {
        if (flag)
            AudioManager.PlayOneShotFromResource("Button", panelOptions.ShowOptionsMenu);
        else
            AudioManager.PlayOneShotFromResource("Button", panelOptions.HideOptionsMenu);
    }

    public void QuitGame()
    {
        AudioManager.PlayOneShotFromResource("Button", Exit);
    }

    void Exit()
    {
        //If we are running in a standalone build of the game
        #if UNITY_STANDALONE
        //Quit the application
        Application.Quit();
        #endif

        //If we are running in the editor
        #if UNITY_EDITOR
        //Stop playing the scene
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
