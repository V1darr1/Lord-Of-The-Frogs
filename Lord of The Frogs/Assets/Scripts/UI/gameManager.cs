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


    [SerializeField] private int playerLevel = 1;
    [SerializeField] private int playerXP = 0;
    [SerializeField] private int gold = 0;

    [SerializeField] private int xpBase = 50;
    [SerializeField] private int xpPerLevel = 25;

    [SerializeField] private int enemiesAlive = 0;


    float timeScaleOrig = 1f;

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

        timeScaleOrig = Time.timeScale;


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

    // --- SETTINGS LOGIC ---

    public void SetFOV(float newFOV)
    {
        currentFOV = newFOV; // Store the new value

        // Apply to the camera in the currently loaded scene
        Camera gameCamera = Camera.main;

        if (gameCamera != null)
        {
            gameCamera.fieldOfView = newFOV;

            // Save to PlayerPrefs for persistence between game sessions
            PlayerPrefs.SetFloat("FOV_Setting", newFOV);
            PlayerPrefs.Save();
        }
    }

    // --- MENU CONTROL LOGIC ---

    public void OpenSettingsMenu()
    {
        // FIX: Hide the Pause Buttons panel before showing the Settings panel
        if (menuPause != null)
        {
            menuPause.SetActive(false);
        }

        // Pause the game and activate the Settings Menu panel
        PauseGame(settingsMenu);
    }

    public void ReturnToPauseMenu(GameObject menu)
    {
        // Hide the current active menu (Settings Menu)
        if (menuActive)
        {
            menuActive.SetActive(false);
        }

        // Explicitly show the Pause Buttons Panel
        if (menuPause != null)
        {
            menuPause.SetActive(true);
        }

        // Reset active menu state
        menuActive = menuPause;

        // Ensure time remains paused
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // ---------- XP / Level ----------
    public void AddXP(int amount)
    {
        if (amount <= 0) return;
        playerXP += amount;

        while (playerXP >= XPNeededForNext())
        {
            playerXP -= XPNeededForNext();
            playerLevel++;
            // TODO: grant stat points, heal, etc. (hook UI here)
        }
        OnXPChanged?.Invoke(playerXP, playerLevel);
        SaveProgress();
    }

    // ---------- Gold ----------
    public void AddGold(int amount)
    {
        gold = Mathf.Max(0, gold + amount);
        OnGoldChanged?.Invoke(gold);
        SaveProgress();
    }

    public bool TrySpendGold(int cost)
    {
        if (gold < cost) return false;
        gold -= cost;
        OnGoldChanged?.Invoke(gold);
        SaveProgress();
        return true;
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