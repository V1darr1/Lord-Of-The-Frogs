using UnityEngine;
using UnityEngine.SceneManagement;

public class gameManager : MonoBehaviour
{
    public static gameManager instance;
    public static bool gameHasBooted = false;


    public static bool shouldOpenSettingsOnLoad = false;

    // Flags and State

    public bool isPaused;

    // UI References
    public GameObject menuPause;
    public GameObject settingsMenu;
    [SerializeField] GameObject menuWin;
    [SerializeField] GameObject menuLose;

    [HideInInspector] public GameObject menuActive;

    // Other Variables
    public bool yInvertON;
    public bool yInvertOFF;
    float timeScaleOrig;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            timeScaleOrig = Time.timeScale;

            // Set initial state
            isPaused = false;
            menuActive = null;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Start()
    {
        // BOOTSTRAP: Load Main Menu first
        if (!gameHasBooted)
        {
            gameHasBooted = true;
            SceneManager.LoadScene("Main Menu");
            return;
        }

        // Check for Options Boot logic
        if (shouldOpenSettingsOnLoad)
        {
            shouldOpenSettingsOnLoad = false;
            OpenSettingsMenu();
        }
        else
        {
            // Normal game start logic
            isPaused = false;
            Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            timeScaleOrig = Time.timeScale;
        }


    }

    void Update()
    {
        // The only reliable way to handle the ESC key is to check the current state 
        // and ONLY allow toggling between NO MENU and the PAUSE MENU.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 1. If NO menu is active, open the PAUSE MENU.
            if (menuActive == null)
            {
                PauseGame(menuPause);
            }
            // 2. If the PAUSE MENU is active, close it (Unpause).
            else if (menuActive == menuPause)
            {
                UnpauseGame();
            }
            // NOTE: If any other menu is active (Settings/Win/Lose), ESC does nothing.
        }
    }

    public void PauseGame(GameObject menu)
    {
        isPaused = true;
        if (menuActive) menuActive.SetActive(false);
        menuActive = menu;
        if (menuActive) menuActive.SetActive(true);
        Time.timeScale = 0;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void UnpauseGame()
    {
        isPaused = false;
        if (menuActive) menuActive.SetActive(false);
        menuActive = null;
        Time.timeScale = timeScaleOrig;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void ReturnToMainMenu()
    {
        gameHasBooted = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene("Main Menu");
    }

    public void OpenWinMenu()
    {
        PauseGame(menuWin);
    }

    public void OpenLoseMenu()
    {
        PauseGame(menuLose);
    }

    public void OpenSettingsMenu()
    {
        PauseGame(settingsMenu);
    }
    //  void CheckWinCondition()
    //  {
    //     if (AllEnemiesAreDefeated())
    //     {

    //         StartCoroutine(ExecuteWinCondition());
    //     }
    //   }
    // IEnumerator ExecuteWinCondition()
    //  {
    //      // Wait for the end of the current frame
    //   yield return new WaitForEndOfFrame();

    // Now, call the function that opens the menu
    //     if (gameManager.instance != null)
    //    {
    //         gameManager.instance.OpenWinMenu();
    //    }
    //  }
}