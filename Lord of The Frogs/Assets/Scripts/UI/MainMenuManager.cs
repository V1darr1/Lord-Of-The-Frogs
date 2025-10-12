using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // Assign these panels in the Inspector of the Main Menu scene
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsMenuPanel;

    private void Start()
    {
        // Plays music when the scene loads (assuming MusicManager is set up correctly)
        //MusicManager.Instance.PlayMusic("MainMenu");

        Time.timeScale = 0f;

        // Ensure initial panel states are correct
        mainMenuPanel.SetActive(true);
        settingsMenuPanel.SetActive(false);
    }

    public void StartNewGame()
    {
        // Set the static flag to prepare the game scene (BP-Dev) for normal start
        gameManager.gameHasBooted = true;
        gameManager.shouldOpenSettingsOnLoad = false;

        // Load the scene where the game starts
        SceneManager.LoadScene("Large_Map_1");
    }

    public void openSettingsMenu()
    {
        // Logic to open the Settings panel within the Main Menu scene
        mainMenuPanel.SetActive(false);
        settingsMenuPanel.SetActive(true);
    }

    public void CloseSettingsPanel()
    {
        // Logic to close the Settings panel and return to the Main Menu view
        settingsMenuPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void ReturnToMainMenu()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene("Main Menu");
    }

    // --- DEBUG FEATURE: Showcase Level Access ---
#if UNITY_EDITOR
    public void LoadShowcaseLevel()
    {
        // Set state flags appropriately for testing
        gameManager.gameHasBooted = true;
        gameManager.shouldOpenSettingsOnLoad = false;

        // Load the dedicated test scene
        SceneManager.LoadScene("ShowcaseLevel");
    }
#endif
    // ---------------------------------------------

    public void QuitGame()
    {
        Application.Quit();

        // Editor-only command to stop play mode instantly
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}