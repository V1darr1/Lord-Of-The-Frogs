using UnityEngine;

public static class DamageInvoker
{
    /// <summary>
    /// Damage only. No physics, no legacy SendMessage hooks.
    /// Expects 'health' on the hit object (or parent).
    /// </summary>
    public static void ApplyHit(GameObject hitObject, int amount, Vector2 hitFrom, float _unused = 0f)
    {
        if (!hitObject) return;

        var hp = hitObject.GetComponent<health>();
        if (!hp) hp = hitObject.GetComponentInParent<health>();
        if (!hp) return;

        // Optional floating numbers
        if (DamageNumberSpawner.Instance != null)
        {
            DamageNumberSpawner.Instance.Spawn(hp.transform.position + Vector3.up * 0.5f, amount);
        }

        hp.ApplyDamage(amount);
    }
}
