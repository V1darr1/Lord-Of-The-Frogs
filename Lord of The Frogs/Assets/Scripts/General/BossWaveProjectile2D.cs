using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BossWaveProjectile2D : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] float lifetime = 5f;
    [SerializeField] bool faceVelocity = true;

    [Header("Orientation")]
    [Tooltip("Angle, in degrees, of your sprite's 'forward' relative to +X (right). " +
             "Use 0 if your sprite points right, 90 if it points up, 180 for left, -90 for down.")]
    [SerializeField] float spriteForwardAngle = 0f;

    [Header("Fairness / Flavor")]
    [SerializeField] float wobbleAmplitude = 0.22f;
    [SerializeField] float wobbleFrequency = 4.5f;

    [Header("Collision")]
    [SerializeField] bool destroyOnHit = true;

    Rigidbody2D rb;
    Vector2 velocity;
    int dmg;
    LayerMask targetMask;

    Vector2 lateral;
    float t;

    void Awake() { rb = GetComponent<Rigidbody2D>(); }
    void OnEnable() { Destroy(gameObject, lifetime); }

    public void Init(Vector2 initialVelocity, int damage, LayerMask target)
    {
        velocity = initialVelocity;
        dmg = damage;
        targetMask = target;

        if (velocity.sqrMagnitude < 0.0001f) velocity = Vector2.right * 8f;
        lateral = new Vector2(-velocity.y, velocity.x).normalized;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = velocity;
#else
        rb.velocity = velocity;
#endif
        ApplyFacingRotation(); // <<< use forward angle when spawned
    }

    void Update()
    {
        t += Time.deltaTime;

        float offset = (wobbleAmplitude > 0f && wobbleFrequency > 0f)
            ? Mathf.Sin(t * Mathf.PI * 2f * wobbleFrequency) * wobbleAmplitude
            : 0f;

        Vector2 basePos =
#if UNITY_6000_0_OR_NEWER
            rb.position;
#else
            rb.position;
#endif
        Vector2 desired = basePos + lateral * offset;
        transform.position = new Vector3(desired.x, desired.y, transform.position.z);

        if (faceVelocity) ApplyFacingRotation(); // <<< keep facing current velocity
    }

    void ApplyFacingRotation()
    {
        Vector2 v =
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity;
#else
            rb.velocity;
#endif
        if (v.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;             // angle of velocity (0 = +X)
        transform.rotation = Quaternion.Euler(0f, 0f, angle - spriteForwardAngle);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & (targetMask.value == 0 ? ~0 : targetMask.value)) == 0)
            return;

        var hp = other.GetComponent<health>();
        if (hp) hp.ApplyDamage(dmg);

        if (destroyOnHit) Destroy(gameObject);
    }
}
