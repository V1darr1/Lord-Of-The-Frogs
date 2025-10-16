using UnityEngine;

public static class DamageInvoker
{
    /// <summary>
    /// Apply damage ONLY. No physics, no legacy SendMessage hooks.
    /// Finds 'health' on this object or a parent, then calls ApplyDamage.
    /// </summary>
    public static void ApplyHit(GameObject hitObject, int amount, Vector2 hitFrom, float _unused = 0f)
    {
        if (!hitObject) return;

        // route to the object that actually has 'health'
        var hp = hitObject.GetComponent<health>();
        if (!hp) hp = hitObject.GetComponentInParent<health>();
        if (!hp) return;

        // floating damage numbers (optional)
        if (DamageNumberSpawner.Instance != null)
        {
            var t = hp.transform; // show where the health component lives
            DamageNumberSpawner.Instance.Spawn(t.position + Vector3.up * 0.5f, amount);
        }

        // ---- DAMAGE ONLY ----
        hp.ApplyDamage(amount);

        // IMPORTANT:
        // - no AddForce
        // - no SendMessage("Hit"/"Damage"/"TakeDamage")
        // If you really need those for some other system, add a bool flag and keep it OFF for enemies.
    }
}