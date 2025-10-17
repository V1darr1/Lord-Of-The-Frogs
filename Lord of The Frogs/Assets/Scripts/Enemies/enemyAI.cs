using System.Collections;
using UnityEngine;

public class enemyAI : MonoBehaviour
{
    public enum EnemyType { Melee, Ranged, Bomber }
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

    [Header("Ranged Settings")]
    [SerializeField] Projectile2D projectilePrefab;
    [SerializeField] float projectileSpeed = 10f;
    [SerializeField] float projectileAttackRange = 6f;
    [SerializeField] Transform muzzle; // optional

    [Header("Bomber (Infected Mosquito)")]
    [SerializeField] float explodeRadius = 1.2f;
    [SerializeField] int explodeDamage = 3;

    [Header("Animation Control")]
    [Tooltip("Use animation events AE_MeleeHit / AE_Fire / AE_AttackEnd / AE_ShootEnd / AE_Explode / AE_DeathEnd if available.")]
    [SerializeField] bool useAnimationEvents = true;
    [Tooltip("Lock movement briefly while attacking/shooting; released by AE_*End or timeout.")]
    [SerializeField] float attackMoveLock = 0.35f;
    [SerializeField] float shootMoveLock = 0.25f;
    [Tooltip("If events/transitions are missing, auto-unlock after this many seconds.")]
    [SerializeField] float attackStateFailSafe = 1.0f;

    [Header("Death / Despawn")]
    [SerializeField] string deathStateName = "Die";
    [SerializeField] int deathLayerIndex = 0;
    [SerializeField] float fallbackDeathTime = 1.0f;
    [SerializeField] bool useUnscaledOnDeath = true;

    [Tooltip("Move to this layer on death to avoid being targeted by the player. Leave blank to skip.")]
    [SerializeField] string corpseLayerName = "Corpse";

    Rigidbody2D rb;
    Transform player;
    Animator anim;
    health hp;

    State state = State.Idle;
    float cdTimer, pathTimer;
    float forgetTimer;
    Vector2 lastKnownPos;

    // Freeze / stagger / death
    float staggerUntil = -999f;
    bool frozen = false;
    bool isDead = false;

    // Attack locks / failsafes
    float moveLockUntil = -999f;
    float stateFailSafeUntil = -999f;

    // Bomber state
    bool hasExploded = false;

    // Despawn guard
    bool despawnStarted = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!hitOrigin) hitOrigin = transform;
        if (!muzzle) muzzle = hitOrigin;
        anim = GetComponent<Animator>();
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;

        hp = GetComponent<health>();
        if (hp) hp.onDeath += OnDeath;
    }

    void OnDestroy()
    {
        if (hp) hp.onDeath -= OnDeath;
    }

    void Update()
    {
        if (isDead || hasExploded || !player) return;

        cdTimer -= Time.deltaTime;
        pathTimer -= Time.deltaTime;

        bool sees = CanSeePlayer(out Vector2 seenPos);
        float dist = sees ? Vector2.Distance(transform.position, seenPos) : Mathf.Infinity;

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

                    if (cdTimer <= 0f)
                    {
                        if (type == EnemyType.Melee && dist <= attackRange)
                        { TryMelee(); cdTimer = attackCooldown; }
                        else if (type == EnemyType.Ranged && dist <= projectileAttackRange)
                        { TryShoot(); cdTimer = attackCooldown; }
                        else if (type == EnemyType.Bomber && dist <= attackRange)
                        { TryExplode(); /*one-way*/ }
                    }
                }
                else
                {
                    forgetTimer -= Time.deltaTime;
                    if (forgetTimer <= 0f) state = State.Return;
                }

                if (dist > loseSightRadius) state = State.Idle;
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
                }
                break;
        }

        // Global failsafe
        if (stateFailSafeUntil > 0f && Time.time > stateFailSafeUntil)
        {
            moveLockUntil = -999f;
            stateFailSafeUntil = -999f;
            if (anim)
            {
                anim.ResetTrigger("Attack");
                anim.ResetTrigger("Shoot");
                anim.ResetTrigger("Explode");
            }
        }
    }

    void FixedUpdate()
    {
        if (isDead || hasExploded || !player)
        {
            SetSpeedParam(0f);
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
            rb.angularVelocity = 0f;
            return;
        }

        if (frozen || Time.time < staggerUntil)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
            rb.angularVelocity = 0f;
            SetSpeedParam(0f);
            return;
        }

        bool lockMove = Time.time < moveLockUntil;

        if (pathTimer > 0f)
        {
            if (lockMove)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector2.zero;
#else
                rb.velocity = Vector2.zero;
#endif
                SetSpeedParam(0f);
                return;
            }

            SetSpeedParam(
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity.sqrMagnitude
#else
                rb.velocity.sqrMagnitude
#endif
            );
            return;
        }
        pathTimer = pathRefresh;

        switch (state)
        {
            case State.Idle:
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector2.zero;
#else
                rb.velocity = Vector2.zero;
#endif
                SetSpeedParam(0f);
                break;

            case State.Chase:
                if (lockMove)
                {
#if UNITY_6000_0_OR_NEWER
                    rb.linearVelocity = Vector2.zero;
#else
                    rb.velocity = Vector2.zero;
#endif
                    SetSpeedParam(0f);
                    break;
                }

                Vector2 toP = (Vector2)player.position - (Vector2)transform.position;
                float stop = (type == EnemyType.Ranged)
                    ? Mathf.Max(stopDistance, projectileAttackRange * 0.5f)
                    : stopDistance;
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = (toP.magnitude > stop) ? toP.normalized * moveSpeed : Vector2.zero;
                SetSpeedParam(rb.linearVelocity.sqrMagnitude);
#else
                rb.velocity = (toP.magnitude > stop) ? toP.normalized * moveSpeed : Vector2.zero;
                SetSpeedParam(rb.velocity.sqrMagnitude);
#endif
                break;

            case State.Return:
                if (lockMove)
                {
#if UNITY_6000_0_OR_NEWER
                    rb.linearVelocity = Vector2.zero;
#else
                    rb.velocity = Vector2.zero;
#endif
                    SetSpeedParam(0f);
                    break;
                }

                Vector2 back = lastKnownPos - (Vector2)transform.position;
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = (back.magnitude > 0.15f) ? back.normalized * moveSpeed : Vector2.zero;
                SetSpeedParam(rb.linearVelocity.sqrMagnitude);
#else
                rb.velocity = (back.magnitude > 0.15f) ? back.normalized * moveSpeed : Vector2.zero;
                SetSpeedParam(rb.velocity.sqrMagnitude);
#endif
                break;
        }
    }

    void SetSpeedParam(float speedSq)
    {
        if (!anim) return;
        anim.SetFloat("Speed", speedSq);
    }

    // ----------------- Attacks -----------------
    void TryMelee()
    {
        if (frozen || Time.time < staggerUntil) return;

        moveLockUntil = Time.time + attackMoveLock;
        stateFailSafeUntil = Time.time + attackStateFailSafe;

        if (anim) { anim.ResetTrigger("Shoot"); anim.ResetTrigger("Explode"); anim.SetTrigger("Attack"); }
        if (!useAnimationEvents) DoMeleeHit();
    }

    void DoMeleeHit()
    {
        int mask = playerMask.value == 0 ? ~0 : playerMask.value;
        var hit = Physics2D.OverlapCircle(hitOrigin.position, attackRange, mask);
        if (!hit) return;

        var thp = hit.GetComponent<health>();
        if (thp) thp.ApplyDamage(damage);
    }

    void TryShoot()
    {
        if (frozen || Time.time < staggerUntil) return;

        moveLockUntil = Time.time + shootMoveLock;
        stateFailSafeUntil = Time.time + attackStateFailSafe;

        if (anim) { anim.ResetTrigger("Attack"); anim.ResetTrigger("Explode"); anim.SetTrigger("Shoot"); }
        if (!useAnimationEvents) DoFire();
    }

    void DoFire()
    {
        if (!projectilePrefab || !player) return;
        Vector2 origin = muzzle ? (Vector2)muzzle.position : (Vector2)hitOrigin.position;
        Vector2 dir = ((Vector2)player.position - origin).normalized;

        Projectile2D proj = Instantiate(projectilePrefab, origin, Quaternion.identity);
        proj.Launch(dir * projectileSpeed, damage, playerMask);
    }

    // -------- Bomber (Infected Mosquito) --------
    void TryExplode()
    {
        if (hasExploded) return;
        hasExploded = true;

        // Hard freeze so it never moves again
        moveLockUntil = float.PositiveInfinity;
        frozen = true;

        if (anim) { anim.ResetTrigger("Attack"); anim.ResetTrigger("Shoot"); anim.SetTrigger("Explode"); }
        if (!useAnimationEvents) DoExplode(); // fallback if no AE_Explode
    }

    void DoExplode()
    {
        // AoE damage
        int mask = playerMask.value == 0 ? ~0 : playerMask.value;
        var hits = Physics2D.OverlapCircleAll(transform.position, explodeRadius, mask);
        foreach (var c in hits)
        {
            var thp = c.GetComponent<health>();
            if (thp) thp.ApplyDamage(explodeDamage);
        }

        // Then proceed to death
        BeginDeathSequence();
    }

    // ----------------- Death / Despawn -----------------
    void OnDeath() { if (!hasExploded) BeginDeathSequence(); }

    void BeginDeathSequence()
    {
        if (isDead) return;
        isDead = true;
        frozen = true;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector2.zero;
#else
        rb.velocity = Vector2.zero;
#endif
        rb.angularVelocity = 0f;

        // >>> Make corpse untargetable immediately <<<
        MakeUntargetable();

        if (anim)
        {
            if (useUnscaledOnDeath) anim.updateMode = AnimatorUpdateMode.UnscaledTime;
            anim.SetBool("Dead", true); // AnyState -> Die
        }

        if (!despawnStarted)
        {
            despawnStarted = true;
            StartCoroutine(WaitForDeathAnimThenDespawn());
        }
    }

    // Preferred: add this event on the last frame of your death clip
    public void AE_DeathEnd()
    {
        if (!despawnStarted)
        {
            despawnStarted = true;
            StartCoroutine(DespawnNow());
        }
    }

    IEnumerator WaitForDeathAnimThenDespawn()
    {
        if (anim)
        {
            float guard = Time.unscaledTime + 2f;
            while (Time.unscaledTime < guard)
            {
                var st = anim.GetCurrentAnimatorStateInfo(deathLayerIndex);
                if (st.IsName(deathStateName)) break;
                yield return null;
            }

            float safety = Time.unscaledTime + Mathf.Max(0.2f, fallbackDeathTime + 2f);
            while (Time.unscaledTime < safety)
            {
                var st = anim.GetCurrentAnimatorStateInfo(deathLayerIndex);
                if (st.IsName(deathStateName) && !st.loop && st.normalizedTime >= 0.99f) break;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(fallbackDeathTime);
        }

        yield return StartCoroutine(DespawnNow());
    }

    IEnumerator DespawnNow()
    {
        yield return null; // finish the frame
        Destroy(gameObject);
    }

    // -------- Make corpse untargetable --------
    void MakeUntargetable()
    {
        // First choice: move to 'Corpse' layer (so player enemyMask won't include it)
        if (!string.IsNullOrEmpty(corpseLayerName))
        {
            int corpseLayer = LayerMask.NameToLayer(corpseLayerName);
            if (corpseLayer >= 0)
            {
                SetLayerRecursively(gameObject, corpseLayer);
                return;
            }
        }

        // Fallback: disable trigger colliders (common pattern for hurtboxes)
        var cols = GetComponentsInChildren<Collider2D>(includeInactive: false);
        foreach (var c in cols)
        {
            if (c == null) continue;
            if (c.isTrigger) c.enabled = false;
        }
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform)
            if (t) SetLayerRecursively(t.gameObject, layer);
    }

    // -------- Animation Events (optional) --------
    public void AE_MeleeHit() { if (!isDead && !hasExploded && type == EnemyType.Melee) DoMeleeHit(); }
    public void AE_Fire() { if (!isDead && !hasExploded && type == EnemyType.Ranged) DoFire(); }
    public void AE_AttackEnd() { moveLockUntil = -999f; stateFailSafeUntil = -999f; }
    public void AE_ShootEnd() { moveLockUntil = -999f; stateFailSafeUntil = -999f; }
    public void AE_Explode() { if (!isDead && type == EnemyType.Bomber) DoExplode(); }

    // --------------- Stagger ---------------
    public void Stagger(float duration)
    {
        if (hasExploded || isDead) return;

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
        if (!hasExploded && !isDead) frozen = false;
    }

    // --------------- Detection ---------------
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
        if (!muzzle) muzzle = hitOrigin;
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(hitOrigin.position, attackRange);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, loseSightRadius);
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.6f); Gizmos.DrawWireSphere(transform.position, explodeRadius);
    }
}
