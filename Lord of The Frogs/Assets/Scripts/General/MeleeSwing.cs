using UnityEngine;
using System.Collections.Generic;

public class MeeleSwing : MonoBehaviour
{
    public int damage = 15;
    public float knockback = 6f;
    public float range = 1.8f;
    public float halfAngle = 35f;
    public LayerMask enemyMask;
    public float cooldown = 0.3f;

    float readyAt;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= readyAt)
        {
            DoSwing();
            readyAt = Time.time + cooldown;
        }
    }

    void DoSwing()
    {
        Vector3 m = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 origin = transform.position;
        Vector2 forward = ((Vector2)m - origin).normalized;

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range, enemyMask);
        var hitSet = new HashSet<GameObject>();

        foreach (var h in hits)
        {
            if (!h) continue;
            Vector2 to = (Vector2)h.transform.position - origin;
            float angle = Vector2.Angle(forward, to);

            if (angle <= halfAngle && !hitSet.Contains(h.gameObject))
            {
                DamageInvoker.ApplyHit(h.gameObject, damage, origin, knockback);
                hitSet.Add(h.gameObject);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
