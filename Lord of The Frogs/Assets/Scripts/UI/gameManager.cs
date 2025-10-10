using UnityEngine;

public class gameManager : MonoBehaviour
{
    // --- SINGLETON AND PERSISTENCE ---
    public static gameManager instance;
    public static bool gameHasBooted = false;
    public bool shouldOpenSettingsOnLoad = false; // Flag set by Main Menu

    // --- UI REFERENCES (Set by UIGameRegistrar.cs) ---
    public GameObject menuPause;
    public GameObject settingsMenu;
    [SerializeField] GameObject menuWin;
    [SerializeField] GameObject menuLose;

    [HideInInspector] public GameObject menuActive;

    // --- GAME STATE AND SETTINGS ---
    public bool isPaused;
    public float currentFOV = 85f; // Persistent FOV setting
    // ... (other game state variables like player, playerHPBar)


    [SerializeField] private int playerLevel = 1;
    [SerializeField] private int playerXP = 0;
    [SerializeField] private int gold = 0;

    [SerializeField] private int xpBase = 50;
    [SerializeField] private int xpPerLevel = 25;

    [SerializeField] private int enemiesAlive = 0;


    float timeScaleOrig = 1f;

    void Awake()
    {
        // 1. Singleton Check
        if (instance == null)
        {
            instance = this;
            // 2. CRITICAL: Persists across scene loads
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Start()
    {
        // 1. BOOTSTRAP LOGIC (Runs in 00_Init)
        if (!gameHasBooted)
        {
            gameHasBooted = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main Menu");
            return;
        }

        // 2. BP-DEV SCENE SETUP (Runs when the game scene loads)

        // Load FOV setting from storage and apply it immediately
        float savedFOV = PlayerPrefs.GetFloat("FOV_Setting", 85f);
        SetFOV(savedFOV);

        if (shouldOpenSettingsOnLoad)
        {
            shouldOpenSettingsOnLoad = false;
            // The UIGameRegistrar starts the process to open settings after a delay
        }
        else
        {
            // Normal game start
            isPaused = false;
            Time.timeScale = timeScaleOrig;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        timeScaleOrig = Time.timeScale;


    }

    void Update()
    {
        // Pause/Unpause logic for Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (menuActive == null)
            {
                PauseGame(menuPause);
            }
            else if (menuActive == menuPause)
            {
                UnpauseGame();
            }
            else if (menuActive == settingsMenu)
            {
                ReturnToPauseMenu(menuPause);
            }
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
        Time.timeScale = 0f;
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
        UnityEngine.SceneManagement.SceneManager.LoadScene("Main Menu");
    }
}