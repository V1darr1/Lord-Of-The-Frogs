using UnityEngine;
using UnityEngine.SceneManagement;

public class gameManager : MonoBehaviour
{
    public static gameManager instance;
    public static bool gameHasBooted = false;

    public bool triggerPause = false;

    [HideInInspector] public bool shouldOpenSettingsOnLoad = false;

    public GameObject menuPause;
    public GameObject settingsMenu;
    [SerializeField] GameObject menuWin;
    [SerializeField] GameObject menuLose;

    [HideInInspector] public GameObject menuActive;

    public bool isPaused;
    public bool yInvertON;
    public bool yInvertOFF;


    [SerializeField] private int playerLevel = 1;
    [SerializeField] private int playerXP = 0;
    [SerializeField] private int gold = 0;

    [SerializeField] private int xpBase = 50;
    [SerializeField] private int xpPerLevel = 25;

    [SerializeField] private int enemiesAlive = 0;


    float timeScaleOrig = 1f;

    float timeScaleOrig;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            timeScaleOrig = Time.timeScale;
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
        if (!gameHasBooted)
        {
            gameHasBooted = true;
            SceneManager.LoadScene("Main Menu");
            return;
        }

        if (shouldOpenSettingsOnLoad)
        {
            shouldOpenSettingsOnLoad = false;
            OpenSettingsMenu();
        }
        else
        {
            isPaused = false;
            Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            timeScaleOrig = Time.timeScale;

            // Note: Player and initial game object setup removed here
        }

        timeScaleOrig = Time.timeScale;


    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            triggerPause = true;
        }
    }
    void LateUpdate()
    {
        if (triggerPause)
        {
            triggerPause = false;

            // Execute the pause/unpause logic
            if (menuActive == null || menuActive == menuPause)
            {
                if (!isPaused)
                {
                    PauseGame(menuPause);
                }
                else
                {
                    UnpauseGame();
                }
            }
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
    //public void AddXP(int amount)
    //{
        //if (amount <= 0) return;
       // playerXP += amount;

        //while (playerXP >= XPNeededForNext())
       // {
         //   playerXP -= XPNeededForNext();
           // playerLevel++;
           // TODO: grant stat points, heal, etc. (hook UI here)
       // }
       // OnXPChanged?.Invoke(playerXP, playerLevel);
       // SaveProgress();
  //  }

    // ---------- Gold ----------
    //public void AddGold(int amount)
  //  {
       // gold = Mathf.Max(0, gold + amount);
       //OnGoldChanged?.Invoke(gold);
        //SaveProgress();
  //  }

   // public bool TrySpendGold(int cost)
   // {
       // if (gold < cost) return false;
       // gold -= cost;
       // OnGoldChanged?.Invoke(gold);
       //SaveProgress();
        //return true;
    //}

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
}