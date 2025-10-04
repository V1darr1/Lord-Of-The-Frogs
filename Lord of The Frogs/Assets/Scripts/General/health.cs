using UnityEngine;

public class health : MonoBehaviour, IDamage
{
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private bool healable = false;

    private float _hp;

    public float CurrentHP => _hp;
    public float MaxHP => maxHP;

    public bool isAlive => _hp > 0f;

    void Awake()
    {
        _hp = Mathf.Max(1f, maxHP);
    }

    public void ApplyDamage(int amount)
    {
        if (!isAlive) return;
        _hp -= amount;
        if (_hp <= 0f) { _hp = 0f; Die(); }
    }

    public void Heal(float amount)
    {
        if (!healable || !isAlive) return;
        _hp = Mathf.Min(maxHP, _hp + amount);
    }

    private void Die()
    {
        if (destroyOnDeath) Destroy(gameObject);
    }
}
