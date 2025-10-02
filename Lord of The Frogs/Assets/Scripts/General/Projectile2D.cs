using UnityEngine;

[RequireComponent (typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile2D : MonoBehaviour
{
    Rigidbody2D rb;
    int dmg;
    LayerMask targetMask;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector2 velocity, int damage, LayerMask target)
    {
        dmg = damage;
        targetMask = target;
        rb.linearVelocity = velocity;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & (targetMask.value == 0 ? ~0 : targetMask.value)) == 0)
            return;

        var hp = other.GetComponent<health>();
        if (hp) hp.ApplyDamge(dmg);

        Destroy(gameObject);
    }
}
