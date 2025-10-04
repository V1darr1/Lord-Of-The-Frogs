using UnityEngine;

public class enemyAI : MonoBehaviour
{
    public enum EnemyType { Melee, Ranged, Bomber, DotDropper }

    public EnemyType type = EnemyType.Melee;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float stopDistance = 0.6f;
    [SerializeField] float pathRefresh = 0.1f;

    [Header("Shared Combat Stats")]
    [SerializeField] int damage = 1;
    [SerializeField] float attackRange = 0.8f;
    [SerializeField] float attackCooldown = 0.6f;
    [SerializeField] LayerMask playerMask;
    [SerializeField] Transform hitOrigin;
    [SerializeField] SpriteRenderer sprite;

    [Header("Ranged")]
    [SerializeField] Projectile2D projectilePrefab;
    [SerializeField] float projectileSpeed = 10f;

    [Header("Bomber")]
    [SerializeField] float explodeRadius = 1.25f;
    [SerializeField] int explodeDamage = 2;

    [Header("DoT Dropper")]
    [SerializeField] DoTZone2D dotZonePrefab;
    [SerializeField] float dropInterval = 2.0f;

    Rigidbody2D rb;
    Transform player;
    float cdTimer;
    float dropTimer;
    float pathTimer;
    bool facingRight;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!sprite) sprite = GetComponent<SpriteRenderer>();
        if (!hitOrigin) hitOrigin = transform;
    }

    void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) player = playerObj.transform;
    }

    void Update()
    {
        if (!player) return;

        cdTimer -= Time.deltaTime;
        dropTimer -= Time.deltaTime;
        pathTimer -= Time.deltaTime;

        //Face target
        float dx = player.position.x - transform.position.x;
        if (dx > 0.02f && !facingRight) SetFacing(true);
        else if (dx < -0.02f && facingRight) SetFacing(false);

        //Attack/Ability by type
        float dist = Vector2.Distance(transform.position, player.position);

        switch (type)
        {
            case EnemyType.Melee:
                if (cdTimer <= 0f && dist <= attackRange) { MeleeHit(); cdTimer = attackCooldown; }
                break;

            case EnemyType.Ranged:
                if (cdTimer <= 0f && dist <= attackRange) { ShootProjectile(); cdTimer = attackCooldown; }
                break;

            case EnemyType.Bomber:
                if (dist <= Mathf.Max(attackRange, explodeRadius * 0.9f)) { Explode(); }
                break;

            case EnemyType.DotDropper:
                if (dropTimer <= 0f) { DropDoT(); dropTimer = dropInterval; }
                if (cdTimer <= 0f && dist <= attackRange) { MeleeHit(); cdTimer = attackCooldown; }
                break;
        }
    }

    private void FixedUpdate()
    {
        if (!player) return;

        if (pathTimer <= 0f)
        {
            pathTimer = pathRefresh;
            Vector2 toPlayer = (player.position - transform.position);
            float dist = toPlayer.magnitude;
            if (dist > stopDistance)
            {
                Vector2 dir = toPlayer.normalized;
                rb.linearVelocity = dir * moveSpeed;
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    void MeleeHit()
    {
        Collider2D hit = Physics2D.OverlapCircle(hitOrigin.position, attackRange, playerMask.value == 0 ? ~0 : playerMask);
        if (!hit) return;

        var hp = hit.GetComponent<health>();
        if (hp) hp.ApplyDamage(damage);
    }

    void ShootProjectile()
    {
        if (!projectilePrefab) return;
        Vector2 dir = (player.position - hitOrigin.position).normalized;
        var proj = Instantiate(projectilePrefab, hitOrigin.position, Quaternion.identity);
        proj.Launch(dir * projectileSpeed, damage, playerMask);
    }

    void Explode()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, explodeRadius, playerMask.value == 0 ? ~0 : playerMask);
        foreach (var h in hits)
        {
            var hp = h.GetComponent<health>();
            if (hp) hp.ApplyDamage(explodeDamage);
        }
        Destroy(gameObject);
    }

    void DropDoT()
    {
        if (!dotZonePrefab) return;
        Instantiate(dotZonePrefab, transform.position, Quaternion.identity);
    }

    void SetFacing(bool right)
    {
        facingRight = right;

        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (right ? 1f : -1f);
        transform.localScale = s;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(hitOrigin ? hitOrigin.position : transform.position, attackRange);
        if (type == EnemyType.Bomber)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, explodeRadius);
        }
    }
}
