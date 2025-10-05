using UnityEngine;
using UnityEngine.UI;

public class FOVSliderController : MonoBehaviour
{
    private Slider fovSlider;

    void Awake()
    {
        // Get the Slider component attached to this GameObject
        fovSlider = GetComponent<Slider>();
    }

    void Start()
    {
        if (fovSlider != null && gameManager.instance != null)
        {
            // 1. Load the persistent value into the UI
            fovSlider.value = gameManager.instance.currentFOV;

            // 2. Add Listener: When the slider moves, call the GameManager's SetFOV method
            // This links the UI event directly to the global logic
            fovSlider.onValueChanged.RemoveAllListeners();
            fovSlider.onValueChanged.AddListener(gameManager.instance.SetFOV);

            // 3. Apply the setting immediately when the scene starts (since we loaded the value)
            gameManager.instance.SetFOV(fovSlider.value);
        }
    }
}