using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    // Options button MUST trigger this function
    public void StartNewGame()
    {
        // 1. Ensure the settings flag is OFF for a normal start
        gameManager.instance.shouldOpenSettingsOnLoad = false;

        // 2. Load the scene where the game starts
        UnityEngine.SceneManagement.SceneManager.LoadScene("BP-Dev");
    }

    public void openSettingsMenu()
    {
        // 1. CRITICAL STEP: Set the flag on the persistent manager
        gameManager.instance.shouldOpenSettingsOnLoad = true;

        // 2. Load the scene where the settings panel is
        UnityEngine.SceneManagement.SceneManager.LoadScene("BP-Dev");
    }
    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}