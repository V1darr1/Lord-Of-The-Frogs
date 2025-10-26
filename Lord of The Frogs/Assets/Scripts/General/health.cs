using System;
using System.Collections;
using UnityEngine;

public class health : MonoBehaviour, IDamage
{
    public static int TotalEnemiesInLevel = 0;

    [SerializeField] private float maxHP = 100f;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private bool healable = false;
    [SerializeField] private AudioClip[] damageSoundClips;
    [SerializeField] bool triggersWinOnDeath = false;

    private float _hp;

    public float CurrentHP => _hp;
    public float MaxHP => maxHP;

    public bool isAlive => _hp > 0f;

    public event Action onDeath;

    void Awake()
    {
        _hp = Mathf.Max(1f, maxHP);
    }

    public void ApplyDamage(int amount)
    {
        if (!isAlive) return;
        _hp -= amount;
        if (_hp <= 0f) { _hp = 0f; Die(); }

        SendMessage("Hit", amount, SendMessageOptions.DontRequireReceiver);



        
        //play sound FX 

        //SoundFXManager.instance.PlaySoundFXClip(damageSoundClip, transform, 1f);
        if (SoundFXManager.instance != null && damageSoundClips != null && damageSoundClips.Length > 0)
        {
            SoundFXManager.instance.PlayRandomSoundFXClip(damageSoundClips, transform, 1f);
        }
        Debug.Log($"[{name}] Took {amount}, HP now {_hp}", this);
    }

    public void Heal(float amount)
    {
        //  Allow healing even if not alive, specifically for respawn logic.
        if (!healable) return;

        // Calculate new HP, capping it at MaxHP.
        _hp = Mathf.Min(maxHP, _hp + amount);


        if (_hp > 0f)
        {

        }
    }

    private void Die()
    {
        if (gameObject.CompareTag("Player") && gameManager.instance)
        {
            // Start the delayed routine for the death animation
            StartCoroutine(OpenLoseMenuAfterDelay(1.5f)); // Wait 1.5 seconds for animation
        }
        // ... (Win condition logic remains) ...

        onDeath?.Invoke();
        if (destroyOnDeath) Destroy(gameObject);
    }

    // Coroutine to wait and then pause the game
    private IEnumerator OpenLoseMenuAfterDelay(float delay)
    {
        // Wait for the duration of the death animation (Time.timeScale is still 1.0)
        yield return new WaitForSeconds(delay);

        // Now freeze the game and show the menu
        if (gameManager.instance != null)
        {
            gameManager.instance.OpenLoseMenu();
        }
    }
}
