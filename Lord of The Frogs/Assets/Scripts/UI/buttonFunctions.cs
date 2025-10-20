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
        // 1. Find and Reset the Player's State using the modern function.
        // We use the Type name (playerController) to find the single persistent instance.
        playerController player = GameObject.FindFirstObjectByType<playerController>();
        if (player != null)
        {
            player.ResetStateForNewGame();
        }

        // 2. Ensure Time is running for the transition.
        Time.timeScale = 1f;

        // 3. Unpause the GameManager state.
        if (gameManager.instance != null)
        {
            gameManager.instance.UnpauseGame();
        }

        // 4. Reload the current scene.
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