using UnityEngine;

public class enemyAI : MonoBehaviour
{
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float stopDistance = 0.6f;
    [SerializeField] int damage = 1;
    [SerializeField] float attackRange = 0.75f;
    [SerializeField] float attackCooldown = 0.6f;
    [SerializeField] LayerMask playerMask;
    [SerializeField] Transform hitOrigin;
    [SerializeField] SpriteRenderer sprite;

    Rigidbody2D rb;
    Transform player;
    float cdTimer;

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
        cdTimer = Time.deltaTime;

        if (sprite && player)
        {
            float dx = player.position.x - transform.position.x;
            if (Mathf.Abs(dx) < 0.02f) sprite.flipX = dx < 0f;
        }

        if (player && cdTimer <= 0f && inAttackRange())
        {
            tryHitPlayer();
            cdTimer = attackCooldown;
        }
    }

    private void FixedUpdate()
    {
        if (!player) return;

        Vector2 toPlayer = (player.position - transform.position);
        float dist = toPlayer.magnitude;
        if (dist > stopDistance)
        {
            Vector2 dir = toPlayer.normalized;
            Vector2 targetPos = (Vector2)transform.position + dir * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(targetPos);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    bool inAttackRange()
    {
        Collider2D hit = Physics2D.OverlapCircle(hitOrigin.position, attackRange, playerMask);
        return hit != null;
    }

    void tryHitPlayer()
    {
        Collider2D hit = Physics2D.OverlapCircle(hitOrigin.position, attackRange, playerMask);
        if (hit)
        {
            health targetHealth = hit.GetComponent<health>();
            if (targetHealth)
            {
                targetHealth.ApplyDamge(damage);
            }
        }
    }
}
