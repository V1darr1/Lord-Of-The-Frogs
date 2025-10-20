using System;
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
        if (!healable || !isAlive) return;
        _hp = Mathf.Min(maxHP, _hp + amount);
    }

    private void Die()
    {
        onDeath?.Invoke();
        if (triggersWinOnDeath && gameManager.instance)
            gameManager.instance.OpenWinMenu();

        if (destroyOnDeath) Destroy(gameObject);
    }
}
