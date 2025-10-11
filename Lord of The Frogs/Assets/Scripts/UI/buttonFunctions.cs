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
        if (gameManager.instance != null)
        {
            // 1. **CRITICAL FIX:** Unfreeze the game time immediately.
            gameManager.instance.UnpauseGame();
        }
        else
        {
            // Fallback for extreme cases where the Singleton is missing
            Time.timeScale = 1f;
        }

        // 2. Reload the currently active scene, which now loads with time scale 1.
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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