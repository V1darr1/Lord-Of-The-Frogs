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
                gameManager.instance.shouldOpenSettingsOnLoad = false; // Reset the flag

                // START THE COROUTINE instead of using Invoke
                StartCoroutine(ExecuteDelayedSettingsOpen());
            }
            else
            {
                // Standard start: make sure the pause menu is hidden
                PausePanel.SetActive(false);
            }
        }
    }

    // Coroutine to wait one full frame before opening the menu
    IEnumerator ExecuteDelayedSettingsOpen()
    {
        // Wait until the very end of the frame (after all other Start/Update logic)
        yield return new WaitForEndOfFrame();

        // This is now guaranteed to run after the game world is stable.
        gameManager.instance.OpenSettingsMenu();
    }
}