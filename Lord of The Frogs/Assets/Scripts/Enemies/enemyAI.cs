using System.Collections;
using UnityEngine;

public class enemyAI : MonoBehaviour
{
    public enum EnemyType { Melee, Ranged }
    enum State { Idle, Chase, Return }

    [Header("Movement")]
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float stopDistance = 0.6f;
    [SerializeField] float pathRefresh = 0.1f;

    [Header("Detection")]
    [SerializeField] float detectionRadius = 6f;
    [SerializeField] float loseSightRadius = 9f;
    [SerializeField] float memoryTime = 2f;
    [SerializeField] LayerMask losBlockers;

    [Header("Combat")]
    public EnemyType type = EnemyType.Melee;
    [SerializeField] int damage = 1;
    [SerializeField] float attackRange = 0.8f;
    [SerializeField] float attackCooldown = 0.6f;
    [SerializeField] Transform hitOrigin;
    [SerializeField] LayerMask playerMask;

    Rigidbody2D rb;
    Transform player;
    State state = State.Idle;
    float cdTimer, pathTimer;
    float forgetTimer;
    Vector2 lastKnownPos;

    // Freeze / stagger
    float staggerUntil = -999f;
    bool frozen = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!hitOrigin) hitOrigin = transform;
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;
    }

    void Update()
    {
        if (!player) return;

        cdTimer -= Time.deltaTime;
        pathTimer -= Time.deltaTime;

        // Sense
        bool sees = CanSeePlayer(out Vector2 seenPos);
        float dist = sees ? Vector2.Distance(transform.position, seenPos) : Mathf.Infinity;

        switch (state)
        {
            case State.Idle:
                if (sees) { state = State.Chase; lastKnownPos = seenPos; forgetTimer = memoryTime; }
                break;

            case State.Chase:
                if (sees) { lastKnownPos = seenPos; forgetTimer = memoryTime; }
                else
                {
                    forgetTimer -= Time.deltaTime;
                    if (forgetTimer <= 0f) state = State.Return;
                }

                if (dist > loseSightRadius) state = State.Idle;

                // fire attacks from Update (but do not move here)
                if (type == EnemyType.Melee && cdTimer <= 0f && dist <= attackRange)
                {
                    TryMelee();
                    cdTimer = attackCooldown;
                }
                break;

            case State.Return:
                if (sees) { state = State.Chase; lastKnownPos = seenPos; forgetTimer = memoryTime; }
                else if (Vector2.Distance(transform.position, lastKnownPos) <= 0.15f)
                { state = State.Idle; }
                break;
        }
    }

    void FixedUpdate()
    {
        if (!player) return;

        // ----- global freeze gate (always first) -----
        if (frozen || Time.time < staggerUntil)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
            rb.angularVelocity = 0f;
            return;
        }

        if (pathTimer > 0f) return;
        pathTimer = pathRefresh;

        switch (state)
        {
            case State.Idle:
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector2.zero;
#else
                rb.velocity = Vector2.zero;
#endif
                break;

            case State.Chase:
                Vector2 toP = (Vector2)player.position - (Vector2)transform.position;
                float stop = stopDistance;
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = (toP.magnitude > stop) ? toP.normalized * moveSpeed : Vector2.zero;
#else
                rb.velocity = (toP.magnitude > stop) ? toP.normalized * moveSpeed : Vector2.zero;
#endif
                break;

            case State.Return:
                Vector2 back = lastKnownPos - (Vector2)transform.position;
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = (back.magnitude > 0.15f) ? back.normalized * moveSpeed : Vector2.zero;
#else
                rb.velocity = (back.magnitude > 0.15f) ? back.normalized * moveSpeed : Vector2.zero;
#endif
                break;
        }
    }

    // --- Combat ---
    void TryMelee()
    {
        if (frozen || Time.time < staggerUntil) return;

        int mask = playerMask.value == 0 ? ~0 : playerMask.value;
        var hit = Physics2D.OverlapCircle(hitOrigin.position, attackRange, mask);
        if (!hit) return;

        var hp = hit.GetComponent<health>();
        if (hp) hp.ApplyDamage(damage);
    }

    // --- Stagger API used by EnemyHitReact ---
    public void Stagger(float duration)
    {
        float d = Mathf.Max(0f, duration);
        frozen = true;
        staggerUntil = Mathf.Max(staggerUntil, Time.time + d);

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector2.zero;
#else
        rb.velocity = Vector2.zero;
#endif
        rb.angularVelocity = 0f;

        StopAllCoroutines();
        StartCoroutine(UnfreezeAfter(d));
    }

    IEnumerator UnfreezeAfter(float d)
    {
        yield return new WaitForSeconds(d);
        frozen = false;
    }

    // --- Helpers ---
    bool CanSeePlayer(out Vector2 seenPos)
    {
        seenPos = Vector2.zero;
        if (!player) return false;

        Vector2 to = (Vector2)player.position - (Vector2)transform.position;
        float dist = to.magnitude;
        if (dist > detectionRadius) return false;

        if (losBlockers.value != 0)
        {
            var hit = Physics2D.Raycast(transform.position, to.normalized, dist, losBlockers);
            if (hit.collider != null) return false;
        }

        int pmask = playerMask.value == 0 ? ~0 : playerMask.value;
        if (((1 << player.gameObject.layer) & pmask) == 0) return false;

        seenPos = player.position;
        return true;
    }

    void OnDrawGizmosSelected()
    {
        if (!hitOrigin) hitOrigin = transform;
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(hitOrigin.position, attackRange);
        Gizmos.color = new Color(0f, 1f, 1f, 0.7f); Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.7f); Gizmos.DrawWireSphere(transform.position, loseSightRadius);
    }
}
