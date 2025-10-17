using UnityEngine;
using UnityEngine.SceneManagement;

public class buttonFunctions : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;
    public void ReturnToPauseMenu()
    {
        if (gameManager.instance != null && pauseMenu != null)
        {
            gameManager.instance.PauseGame(pauseMenu);
        }
        else
        {
            Debug.LogError("ReturnToPauseMenu Failed");
        }
    }
    // Resume button calls UnpauseGame() which handles time and cursor.
    public void resume()
    {
        if (gameManager.instance != null)
        {
            gameManager.instance.UnpauseGame();
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        if (gameManager.instance)
        {
            if (gameManager.instance != null)
            {
                gameManager.instance.UnpauseGame();
            }
            gameManager.instance.isPaused = false;
            gameManager.instance.menuActive = null;
        }

        SceneManager.LoadScene("Large_Map_1");

        // 3. Reload the current scene
        //SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        if (gameManager.instance != null)
        {
            gameManager.instance.ReturnToMainMenu();
        }
    }

    public void openSettingsMenu()
    {
        if (gameManager.instance != null)
        {
            gameManager.instance.OpenSettingsMenu();
        }
    }

    public void quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}