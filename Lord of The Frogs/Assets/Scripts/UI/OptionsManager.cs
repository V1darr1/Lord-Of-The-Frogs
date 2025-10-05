using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionsManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    public void openSettingsMenu()
    {
        gameManager.instance.OpenSettingsMenu();
    }
    public void ReturnToMainMenu()
    {

        SceneManager.LoadScene("Scenes/Main Menu");

        //MusicManager.Instance.PlayMusic("MainMenu");
    }

    //  public void InvertYOn()
    //  {
    //    if (cameraController != null) cameraController.SetInvertY(true);
    // }

    // public void InvertYOff()
    // {
    //  if (cameraController != null) cameraController.SetInvertY(false);

    // }
}