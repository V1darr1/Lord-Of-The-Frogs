using System.Collections; // <--- ADD THIS for Coroutines!
using UnityEngine;

public class UIGameRegistrar : MonoBehaviour
{
    public GameObject SettingsPanel;
    public GameObject PausePanel;

    void Start()
    {
        if (gameManager.instance != null)
        {
            // 1. RE-ESTABLISH THE LINKS
            gameManager.instance.settingsMenu = SettingsPanel;
            gameManager.instance.menuPause = PausePanel;

            // 2. CHECK THE FLAG and EXECUTE THE REQUEST
            if (gameManager.instance.shouldOpenSettingsOnLoad)
            {


                // START THE COROUTINE 
                StartCoroutine(ExecuteDelayedSettingsOpen());
            }
            else
            {
                // make sure the pause menu is hidden
                PausePanel.SetActive(false);
            }
        }
    }

    // Coroutine to wait one full frame before opening the menu
    IEnumerator ExecuteDelayedSettingsOpen()
    {
        // Wait until the very end of the frame 
        yield return new WaitForEndOfFrame();

        //  run after the game world is stable.
        if (gameManager.instance != null)
        {
            gameManager.instance.OpenSettingsMenu();

        }
    }
}