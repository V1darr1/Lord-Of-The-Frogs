using UnityEngine;
using UnityEngine.UI;

public class ResourceBarUI : MonoBehaviour
{
    [SerializeField] private health targetHealth;
    [SerializeField] private Image fillImage;

    void Update()
    {
        if (targetHealth)
            fillImage.fillAmount = targetHealth.CurrentHP / targetHealth.MaxHP;
    }
}
