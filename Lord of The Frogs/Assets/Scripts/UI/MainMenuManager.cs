using UnityEngine;
public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsMenuPanel;
    // Options button MUST trigger this function
    public void StartNewGame()
    {
        gameManager.gameHasBooted = true;
        if (gameManager.instance != null)
        {

            gameManager.instance.shouldOpenSettingsOnLoad = false;
        }

        // Load the scene where the game starts
        UnityEngine.SceneManagement.SceneManager.LoadScene("BP-Dev");
    }

    public void openSettingsMenu()
    {

        mainMenuPanel.SetActive(false);
        settingsMenuPanel.SetActive(true);
    }
    public void CloseSettingsPanel()
    {
        settingsMenuPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}