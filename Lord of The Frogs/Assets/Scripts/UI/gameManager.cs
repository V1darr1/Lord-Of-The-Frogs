using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class gameManager : MonoBehaviour
{
    // -------- Singleton / Boot --------
    public static gameManager instance;
    public static bool gameHasBooted = false;
    public static bool shouldOpenSettingsOnLoad = false;

    [Header("References (optional)")]
    [Tooltip("If assigned, this Player object will be marked DontDestroyOnLoad with the manager.")]
    public GameObject player;

    [Header("Menus")]
    public GameObject menuPause;
    public GameObject settingsMenu;
    [SerializeField] public GameObject menuWin;
    [SerializeField] public GameObject menuLose;

    [HideInInspector] public GameObject menuActive;

    [Header("State")]
    public bool isPaused;

    private float timeScaleOrig = 1f;

    private static readonly string[] MenuScenes = { "Main Menu", "Options" };

    private bool IsMenuScene(Scene s) => MenuScenes.Contains(s.name);

    // ---------- Lifecycle ----------
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            if (player) DontDestroyOnLoad(player);


        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Start()
    {
        //  load Main Menu and return.
        if (!gameHasBooted)
        {
            gameHasBooted = true;
            SceneManager.LoadScene("Main Menu");
            return;
        }



        // 1. Reset Time and Pause State 
        isPaused = false;
        Time.timeScale = 1f;
        menuActive = null;
        health.TotalEnemiesInLevel = 0; // Reset enemy counter
        StartCoroutine(AggressiveMenuCleanup());

        if (menuPause) menuPause.SetActive(false);
        if (menuWin) menuWin.SetActive(false);
        if (menuLose) menuLose.SetActive(false);
        if (settingsMenu) settingsMenu.SetActive(false);
        menuActive = null;



        if (shouldOpenSettingsOnLoad)
        {
            shouldOpenSettingsOnLoad = false;

            OpenSettingsMenu();
        }
        else
        {

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

    }
    private IEnumerator AggressiveMenuCleanup()
    {
        // Wait until the end of the first frame (when all objects are guaranteed to be initialized)
        yield return null;

        // Now execute the aggressive cleanup:
        if (menuPause) menuPause.SetActive(false);
        if (menuWin) menuWin.SetActive(false);
        if (menuLose) menuLose.SetActive(false);
        if (settingsMenu) settingsMenu.SetActive(false);
        menuActive = null;
    }
    // ---------- Scene Loaded ----------
    private void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        health.TotalEnemiesInLevel = 0;

        if (IsMenuScene(s))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            return;
        }

        isPaused = false;
        menuActive = null;
        Time.timeScale = 1f;
        timeScaleOrig = 1f;

        var pgo = player ? player : GameObject.FindGameObjectWithTag("Player");
        if (!pgo) { Debug.LogWarning("[GM Spawn] No Player found after scene load."); return; }

        var spawns = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);
        if (spawns == null || spawns.Length == 0) { Debug.LogWarning("[GM Spawn] No PlayerSpawnPoint found in this scene."); return; }

        string requested = ScenePortal.NextSpawnId;
        PlayerSpawnPoint chosen = null;

        if (!string.IsNullOrEmpty(requested))
            chosen = spawns.FirstOrDefault(p => p && p.id == requested);

        if (chosen == null)
        {
            chosen = spawns.FirstOrDefault(p => p && p.id == "LM1") ?? spawns[0];
            if (!string.IsNullOrEmpty(requested))
                Debug.LogWarning($"[GM Spawn] Requested '{requested}' not found. Using '{chosen.id}'.");
        }

        var w = chosen.transform.position;
        var cur = pgo.transform.position;
        pgo.transform.position = new Vector3(w.x, w.y, cur.z);

        var rb2d = pgo.GetComponent<Rigidbody2D>();
        if (rb2d)
        {
#if UNITY_6000_0_OR_NEWER
            rb2d.linearVelocity = Vector2.zero;
#else
        rb2d.velocity = Vector2.zero;
#endif
            rb2d.angularVelocity = 0f;
        }

        ScenePortal.NextSpawnId = null;
        Debug.Log($"[GM Spawn] Scene '{s.name}' → '{chosen.id}' @ {w}");

        // ---- Ensure UNPAUSED again next frame (beats late menu code) ----
        StartCoroutine(EnsureUnpausedNextFrame());
    }

    private System.Collections.IEnumerator EnsureUnpausedNextFrame()
    {
        yield return null;
        isPaused = false;
        menuActive = null;
        Time.timeScale = 1f;
        Debug.Log($"[GM] Post-frame unpause: isPaused={isPaused}, timeScale={Time.timeScale}");
    }

    // ---------- Update (ESC handling) ----------
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (menuActive == null) PauseGame(menuPause);
            else if (menuActive == menuPause) UnpauseGame();
        }
    }

    // ---------- Menu / Pause ----------
    public void PauseGame(GameObject menuToShow)
    {
        if (menuToShow == null) return;

        isPaused = true;
        timeScaleOrig = Time.timeScale;
        Time.timeScale = 0f;

        if (menuActive && menuActive != menuToShow) menuActive.SetActive(false);

        menuActive = menuToShow;
        menuActive.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        playerController pc = GameObject.FindFirstObjectByType<playerController>();
        if (pc != null) pc.SetInputEnabled(false);
    }

    public void UnpauseGame()
    {
        isPaused = false;

        if (menuActive) menuActive.SetActive(false);
        menuActive = null;

        Time.timeScale = timeScaleOrig > 0f ? timeScaleOrig : 1f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        playerController pc = GameObject.FindFirstObjectByType<playerController>();
        if (pc != null) pc.SetInputEnabled(true);
    }

    public void OpenSettingsMenu()
    {
        if (!settingsMenu) return;

        // Hide Pause buttons if open
        if (menuPause) menuPause.SetActive(false);

        PauseGame(settingsMenu);
    }
    public void CloseSettingsMenu()
    {

        if (menuActive == settingsMenu && menuPause)
        {
            //  Options -> Pause Menu. 
            // hide settings and show pause
            menuActive.SetActive(false);
            menuPause.SetActive(true);
            menuActive = menuPause;
        }
        else
        {
            //  Options -> Resume 

            UnpauseGame();
        }
    }

    public void ReturnToPauseMenu()
    {
        if (menuActive) menuActive.SetActive(false);
        if (menuPause) menuPause.SetActive(true);
        menuActive = menuPause;

        isPaused = true;
        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void OpenWinMenu(bool pauseTime = true)
    {
        if (!menuWin) return;

        if (menuActive) menuActive.SetActive(false);
        menuActive = menuWin;
        menuActive.SetActive(true);

        if (pauseTime)
        {
            isPaused = true;
            timeScaleOrig = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            isPaused = false;
            Time.timeScale = timeScaleOrig;
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void OpenLoseMenu()
    {
        if (!menuLose) return;

        if (menuActive) menuActive.SetActive(false);
        menuActive = menuLose;
        menuActive.SetActive(true);

        isPaused = true;
        timeScaleOrig = Time.timeScale;
        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ReturnToMainMenu()
    {
        gameHasBooted = false;
        isPaused = false;
        menuActive = null;


        Time.timeScale = 1f;

        // Set cursor state for the Main Menu scene
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Load the final destination scene
        SceneManager.LoadScene("Main Menu");
    }
}
