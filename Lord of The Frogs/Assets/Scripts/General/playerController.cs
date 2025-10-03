using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class playerController : MonoBehaviour, IDamage
{
    [SerializeField] Transform attackOrigin;
    [SerializeField] float attackRange = 1.4f;
    [SerializeField] float attackArcDeg = 110f;
    [SerializeField] int attackDamage = 20;
    [SerializeField] float attackCooldown = 0.25f;
    [SerializeField] LayerMask enemyMask;

    public float moveSpeed;
    
    private Animator anim;
    private Vector2 moveDir;
    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private health hp;

    float attackTimer;
    bool facingRight = true;
    Vector2 faceDir = Vector2.right;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        hp = GetComponent<health>();
    }

    void Update()
    {
        Move();
        Animate();

        if (rb.linearVelocity.sqrMagnitude > 0.001f)
        {
            faceDir = rb.linearVelocity.normalized;
        }

        attackTimer -= Time.deltaTime;
        if (Input.GetMouseButtonDown(0) && attackTimer <= 0f)
        {
            AttackCone();
            attackTimer = attackCooldown;
            if (anim) anim.SetTrigger("Attack");
        }
    }

    private void Move()
    {
        float hor = Input.GetAxisRaw("Horizontal");
        float ver = Input.GetAxisRaw("Vertical");

        moveDir = new Vector2(hor, ver).normalized;
        rb.linearVelocity = moveDir * moveSpeed;

        if (hor > 0.02f && !facingRight) SetFacing(true);
        else if (hor < -0.02f && facingRight) SetFacing(false);
    }

    void SetFacing(bool right)
    {
        facingRight = right;

        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (right ? 1f : -1f);
        transform.localScale = s;
    }

    private void Animate()
    {
        if (!anim) return;
        anim.SetFloat("Movement X", moveDir.x);
        anim.SetFloat("Movement Y", moveDir.y);
        anim.SetFloat("Speed", rb.linearVelocity.sqrMagnitude);
    }

    public void ApplyDamge(int amount)
    {
        if (hp) hp.ApplyDamge(amount);
    }

    void AttackOnce()
    {
        var hit = Physics2D.OverlapCircle(attackOrigin ? attackOrigin.position : transform.position,
                                          attackRange, enemyMask.value == 0 ? ~0 : enemyMask.value);
        if (!hit) return;

        var hp = hit.GetComponent<health>();
        if (hp) hp.ApplyDamge(attackDamage);

        if (anim) anim.SetTrigger("Attack");
    }

    void AttackCone()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyMask.value == 0 ? ~0 : enemyMask.value);
        if (hits == null || hits.Length == 0) return;

        float cosHalf = Mathf.Cos(0.5f * attackArcDeg * Mathf.Deg2Rad);

        foreach (var h in hits)
        {
            Vector2 to = (Vector2)h.bounds.center - (Vector2)transform.position;
            float dist = to.magnitude;
            if (dist <= 0.001f) continue;

            Vector2 dir = to / dist;

            if (Vector2.Dot(dir, faceDir) >= cosHalf)
            {
                var hp = h.GetComponent<health>();
                if (hp) hp.ApplyDamge(attackDamage);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // draw cone rays
        Vector3 center = transform.position;
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
        Gizmos.DrawLine(center, center + (Vector3)(left * attackRange));
        Gizmos.DrawLine(center, center + (Vector3)(right * attackRange));
    }
}
