using UnityEngine;

public class UIGameRegistrar : MonoBehaviour
{
    // Assign these panels in the Inspector of the BP-Dev scene
    public GameObject SettingsPanel;
    public GameObject PausePanel;

    void Start()
    {

        if (gameManager.instance != null)
        {

            gameManager.instance.settingsMenu = SettingsPanel;
            gameManager.instance.menuPause = PausePanel;



        }
        else
        {

            PausePanel.SetActive(false);
        }
    }
}

