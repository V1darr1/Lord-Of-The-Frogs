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

    public System.Action OnRoomCleared;

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

        //MusicManager.Instance.PlayMusic("Play Music");
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