using UnityEngine;

public class enemyAI : MonoBehaviour
{
    public enum EnemyType { Melee, Ranged, Bomber, DotDropper }
    enum State { Idle, Chase, Return }

    public EnemyType type = EnemyType.Melee;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float stopDistance = 0.6f;
    [SerializeField] float pathRefresh = 0.1f;

    [Header("Detection")]
    [SerializeField] float detectionRadius = 6f;
    [SerializeField] bool useVisionCone = true;
    [SerializeField] float visionConeDeg = 110f;
    [SerializeField] LayerMask losBlockers;
    [SerializeField] float loseSightRadius = 9f;
    [SerializeField] float memoryTime = 2.0f;

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
    [SerializeField] float primeRadius;
    [SerializeField] float fuseTime;
    [SerializeField] float bomberSpeedBoost;

    [Header("DoT Dropper")]
    [SerializeField] DoTZone2D dotZonePrefab;
    [SerializeField] float dropInterval = 2.0f;

    //[SerializeField] float defaultStagger = 0.15f;
    float staggerUntil;

    Rigidbody2D rb;
    Transform player;
    float cdTimer;
    float dropTimer;
    float pathTimer;
    bool facingRight;
    State state = State.Idle;

    bool primed;
    float fuse;

    float forgetTimer;
    Vector2 lastKnownPos;

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

        if (Time.time < staggerUntil) return;

        cdTimer -= Time.deltaTime;
        dropTimer -= Time.deltaTime;
        pathTimer -= Time.deltaTime;

        // ——— SENSE ———
        bool sees = CanSeePlayer(out Vector2 seenPos);
        float distToPlayer = Vector2.Distance(transform.position, player.position);

        switch (state)
        {
            case State.Idle:
                if (sees)
                {
                    state = State.Chase;
                    lastKnownPos = seenPos;
                    forgetTimer = memoryTime;
                }
                break;

            case State.Chase:
                if (sees)
                {
                    lastKnownPos = seenPos;
                    forgetTimer = memoryTime;

                    // Flip towards player horizontally
                    float dx = player.position.x - transform.position.x;
                    if (dx > 0.02f && !facingRight) SetFacing(true);
                    else if (dx < -0.02f && facingRight) SetFacing(false);

                    // Abilities/attacks
                    if ((type == EnemyType.Melee || type == EnemyType.DotDropper) && cdTimer <= 0f && distToPlayer <= attackRange)
                    { MeleeHit(); cdTimer = attackCooldown; }

                    if (type == EnemyType.Ranged && cdTimer <= 0f && distToPlayer <= attackRange * 2.5f)
                    { ShootProjectile(); cdTimer = attackCooldown; }

                    if (type == EnemyType.DotDropper && dropTimer <= 0f)
                    { DropDoT(); dropTimer = dropInterval; }

                    if (type == EnemyType.Bomber)
                    {
                        if (distToPlayer <= explodeRadius) { Explode(); return; }
                        if (!primed && distToPlayer <= primeRadius) { primed = true; fuse = fuseTime; }
                    }
                }
                else
                {
                    forgetTimer -= Time.deltaTime;
                    if (forgetTimer <= 0f) state = State.Return;
                }

                // hard de-aggro by distance
                if (distToPlayer > loseSightRadius)
                {
                    state = State.Idle;
                    primed = false;
                }
                break;

            case State.Return:
                if (sees)
                {
                    state = State.Chase;
                    lastKnownPos = seenPos;
                    forgetTimer = memoryTime;
                }
                else if (Vector2.Distance(transform.position, lastKnownPos) <= 0.15f)
                {
                    state = State.Idle;
                    primed = false;
                }
                break;
        }

        // bomber fuse keeps ticking once primed
        if (type == EnemyType.Bomber && primed)
        {
            fuse -= Time.deltaTime;
            if (fuse <= 0f) { Explode(); return; }
        }
    }

    void FixedUpdate()
    {
        if (!player) return;

        if (Time.time < staggerUntil) { rb.linearVelocity = Vector2.zero; return; }

        if (pathTimer > 0f) return;
        pathTimer = pathRefresh;

        switch (state)
        {
            case State.Idle:
                rb.linearVelocity = Vector2.zero;
                break;

            case State.Chase:
                if (type == EnemyType.Bomber)
                {
                    Vector2 to = (Vector2)player.position - (Vector2)transform.position;
                    if (to.magnitude > 0.05f)
                    {
                        float spd = moveSpeed * (primed ? bomberSpeedBoost : 1f);
                        rb.linearVelocity = to.normalized * spd;
                    }
                    else rb.linearVelocity = Vector2.zero;
                    break;
                }

                Vector2 tp = (Vector2)player.position - (Vector2)transform.position;
                float stop = (type == EnemyType.Ranged) ? Mathf.Max(stopDistance, attackRange * 0.7f) : stopDistance;
                rb.linearVelocity = (tp.magnitude > stop) ? tp.normalized * moveSpeed : Vector2.zero;
                break;

            case State.Return:
                Vector2 back = lastKnownPos - (Vector2)transform.position;
                rb.linearVelocity = (back.magnitude > 0.15f) ? back.normalized * moveSpeed : Vector2.zero;
                break;
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

    bool CanSeePlayer(out Vector2 seenPos)
    {
        seenPos = Vector2.zero;
        if (!player) return false;

        Vector2 to = (Vector2)player.position - (Vector2)transform.position;
        float dist = to.magnitude;
        if (dist > detectionRadius) return false;

        if (useVisionCone)
        {
            Vector2 fwd = facingRight ? Vector2.right : Vector2.left;
            if (Vector2.Angle(fwd, to) > visionConeDeg * 0.5f) return false;
        }

        // line of sight (if mask specified)
        if (losBlockers.value != 0)
        {
            var hit = Physics2D.Raycast(transform.position, to.normalized, dist, losBlockers);
            if (hit.collider != null) return false;
        }

        // ensure player's layer is intended target
        int pmask = playerMask.value == 0 ? ~0 : playerMask.value;
        if (((1 << player.gameObject.layer) & pmask) == 0) return false;

        seenPos = player.position;
        return true;
    }

    void SetFacing(bool right)
    {
        facingRight = right;

        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (right ? 1f : -1f);
        transform.localScale = s;
    }

    public void Stagger(float duration)
    {
        staggerUntil = Mathf.Max(staggerUntil, Time.time + duration);
        if (rb) rb.linearVelocity = Vector2.zero;
    }

    void OnDrawGizmosSelected()
    {
        if (!hitOrigin) hitOrigin = transform;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(hitOrigin.position, attackRange);

        Gizmos.color = new Color(0f, 1f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, loseSightRadius);

        if (type == EnemyType.Bomber)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, explodeRadius);
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, primeRadius);
        }
    }
}
