using UnityEngine;

public class PlayerControllerCombat2D : MonoBehaviour, IDamage
{
    [Header("Move")]
    [SerializeField] float moveSpeed = 5f;

    [Header("Melee (mouse-aimed cone)")]
    [SerializeField] float attackRange = 1.6f;     // radius of cone
    [SerializeField] float attackArcDeg = 110f;    // cone width
    [SerializeField] int attackDamage = 20;
    [SerializeField] float attackCooldown = 0.25f;
    [SerializeField] float knockback = 6f;
    [SerializeField] LayerMask enemyMask;

    [Header("Optional")]
    [SerializeField] Transform attackOrigin;       // if null, uses player position
    [SerializeField] string attackTrigger = "Attack";
    [SerializeField] bool rotateAttackOrigin = true;

    Rigidbody2D rb;
    Animator anim;
    health hp;
    Camera cam;

    float attackTimer;
    bool facingRight = true;
    Vector2 moveDir;
    Vector2 faceDir = Vector2.right;               // ALWAYS driven by mouse

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        hp = GetComponent<health>();
        cam = Camera.main;
    }

    void Update()
    {
        UpdateAimFromMouse();        // <— mouse controls facing
        HandleMove();
        HandleAttack();
        UpdateAnim();
    }

    // ---------- Mouse Aim ----------
    void UpdateAimFromMouse()
    {
        if (!cam) cam = Camera.main;
        Vector3 m = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 toMouse = (Vector2)m - (Vector2)transform.position;

        if (toMouse.sqrMagnitude > 0.0001f)
            faceDir = toMouse.normalized;

        // Flip entire character so children & hitboxes follow
        if (faceDir.x > 0.02f && !facingRight) SetFacing(true);
        else if (faceDir.x < -0.02f && facingRight) SetFacing(false);

        // rotate attack origin
        if (rotateAttackOrigin && attackOrigin)
        {
            float ang = Mathf.Atan2(faceDir.y, faceDir.x) * Mathf.Rad2Deg;
            attackOrigin.rotation = Quaternion.Euler(0, 0, ang);
        }
    }

    void SetFacing(bool right)
    {
        facingRight = right;
        var s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (right ? 1f : -1f);
        transform.localScale = s;
    }

    // ---------- Movement ----------
    void HandleMove()
    {
        float hor = Input.GetAxisRaw("Horizontal");
        float ver = Input.GetAxisRaw("Vertical");

        moveDir = new Vector2(hor, ver).normalized;
        rb.linearVelocity = moveDir * moveSpeed;
    }

    // ---------- Attack ----------
    void HandleAttack()
    {
        attackTimer -= Time.deltaTime;
        if (Input.GetMouseButtonDown(0) && attackTimer <= 0f)
        {
            AttackCone();
            attackTimer = attackCooldown;
            if (anim && !string.IsNullOrEmpty(attackTrigger))
                anim.SetTrigger(attackTrigger);
        }
    }

    void AttackCone()
    {
        Vector2 origin = attackOrigin ? (Vector2)attackOrigin.position : (Vector2)transform.position;

        // Collect by radius first, then filter by angle
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            origin, attackRange,
            enemyMask.value == 0 ? ~0 : enemyMask.value
        );
        if (hits == null || hits.Length == 0) return;

        float cosHalf = Mathf.Cos(0.5f * attackArcDeg * Mathf.Deg2Rad);

        foreach (var h in hits)
        {
            if (!h) continue;
            Vector2 to = (Vector2)h.bounds.center - origin;
            float mag = to.magnitude;
            if (mag < 0.0001f) continue;

            Vector2 dir = to / mag;
            if (Vector2.Dot(dir, faceDir) >= cosHalf)
            {
                DamageInvoker.ApplyHit(h.gameObject, attackDamage, origin, knockback);
            }
        }
    }

    // ---------- Animation ----------
    void UpdateAnim()
    {
        if (!anim) return;
        anim.SetFloat("Movement X", moveDir.x);
        anim.SetFloat("Movement Y", moveDir.y);
        anim.SetFloat("Speed", rb.linearVelocity.sqrMagnitude);
    }

    // ---------- Taking damage ----------
    public void ApplyDamage(int amount)
    {
        if (hp) hp.ApplyDamage(amount);
    }

    // ---------- Gizmos ----------
    void OnDrawGizmosSelected()
    {
        Vector2 origin = attackOrigin ? (Vector2)attackOrigin.position : (Vector2)transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin, attackRange);

        Vector2 f = faceDir.sqrMagnitude < 0.001f ? Vector2.right : faceDir.normalized;
        float half = 0.5f * attackArcDeg * Mathf.Deg2Rad;

        Vector2 left = new Vector2(
            f.x * Mathf.Cos(half) - f.y * Mathf.Sin(half),
            f.x * Mathf.Sin(half) + f.y * Mathf.Cos(half)
        );
        Vector2 right = new Vector2(
            f.x * Mathf.Cos(-half) - f.y * Mathf.Sin(-half),
            f.x * Mathf.Sin(-half) + f.y * Mathf.Cos(-half)
        );
        Gizmos.DrawLine(origin, origin + left * attackRange);
        Gizmos.DrawLine(origin, origin + right * attackRange);
    }
}
