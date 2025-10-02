using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class playerController : MonoBehaviour, IDamage
{
    [SerializeField] Transform attackOrigin;
    [SerializeField] float attackRange = 0.75f;
    [SerializeField] int attackDamage = 20;
    [SerializeField] LayerMask enemyMask;
    [SerializeField] float attackCooldown = 0.25f;
    float attackTimer;

    public float moveSpeed;
    
    private Animator anim;
    private Vector2 moveDir;
    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private health hp;

    bool facingRight = true;

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

        attackTimer -= Time.deltaTime;
        if (Input.GetMouseButtonDown(0) && attackTimer <= 0)
        {
            AttackOnce();
            attackTimer = attackCooldown;
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
}
