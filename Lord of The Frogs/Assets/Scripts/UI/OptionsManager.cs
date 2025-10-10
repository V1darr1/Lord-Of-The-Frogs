using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionsManager : MonoBehaviour
{
    // Awake runs before Start and is the most reliable place for initialization
    void Awake()
    {

        SceneManager.LoadScene("Main Menu");
    }

    // Start is now correctly left empty
    void Start()
    {

    }
}
