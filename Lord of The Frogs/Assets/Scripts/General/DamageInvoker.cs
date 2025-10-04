using UnityEngine;

public static class DamageInvoker
{
 
    // Applies damage to a target. 
    // Tries common method names (ApplyDamage, ApplyDamge, TakeDamage, Damage, Hit)
    public static void ApplyHit(GameObject target, int amount, Vector2 hitFrom, float knockbackForce = 0f)
    {
        if (!target) return;

        if (DamageNumberSpawner.Instance != null)
        {
            DamageNumberSpawner.Instance.Spawn(target.transform.position + Vector3.up * 0.5f, amount);
        }


        var rb = target.GetComponent<Rigidbody2D>();
        if (rb && knockbackForce > 0f)
        {
            Vector2 dir = ((Vector2)target.transform.position - hitFrom).normalized;
            rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
        }


        target.SendMessage("ApplyDamage", amount, SendMessageOptions.DontRequireReceiver);
        target.SendMessage("TakeDamage", amount, SendMessageOptions.DontRequireReceiver);
        target.SendMessage("Damage", amount, SendMessageOptions.DontRequireReceiver);
        target.SendMessage("Hit", amount, SendMessageOptions.DontRequireReceiver);
    }
}
