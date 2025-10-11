using System;
using UnityEngine;

public class health : MonoBehaviour, IDamage
{
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private bool healable = false;
    [SerializeField] private AudioClip[] damageSoundClips;

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

        //playm sound FX 

        //SoundFXManager.instance.PlaySoundFXClip(damageSoundClip, transform, 1f);
        SoundFXManager.instance.PlayRandomSoundFXClip(damageSoundClips, transform, 1f);
    }

    public void Heal(float amount)
    {
        if (!healable || !isAlive) return;
        _hp = Mathf.Min(maxHP, _hp + amount);
    }

    private void Die()
    {
        onDeath?.Invoke();
        
        if (destroyOnDeath) Destroy(gameObject);
    }
}
