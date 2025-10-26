using UnityEngine;

public class HurtFlashOnDamage : MonoBehaviour
{
    [SerializeField] private HurtFlashOnMainCamera cameraFlash; // drag Main Camera here

    public void ApplyDamage(int amount) { cameraFlash?.TriggerHurt(); }
    public void ApplyDamge(int amount) { cameraFlash?.TriggerHurt(); } // typo cover
    public void TakeDamage(int amount) { cameraFlash?.TriggerHurt(); }
    public void Damage(int amount) { cameraFlash?.TriggerHurt(); }
    public void Hit(int amount) { cameraFlash?.TriggerHurt(); }
}
