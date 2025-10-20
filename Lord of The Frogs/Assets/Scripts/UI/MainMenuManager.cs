using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // Assign these panels in the Inspector of the Main Menu scene
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject settingsMenuPanel;

    private void Start()
    {



        mainMenuPanel.SetActive(true);
        settingsMenuPanel.SetActive(false);


        if (creditsPanel != null)
        {
            creditsPanel.SetActive(false);
        }
    }

    public void StartNewGame()
    {

        gameManager.gameHasBooted = true; // Tell the game it's past the main menu
        gameManager.shouldOpenSettingsOnLoad = false;

        // Ensure Time is running for the scene transition
        Time.timeScale = 1f;

        // Load the final game scene directly
        SceneManager.LoadScene("Large_Map_1");
    }
    public void OpenCredits()
    {
        // Hide the main panel and show the credits panel
        mainMenuPanel.SetActive(false);
        settingsMenuPanel.SetActive(false); // Ensure settings is also hidden
        creditsPanel.SetActive(true);
    }

    public void CloseCredits()
    {
        // Hide the credits panel and show the main panel
        creditsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
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