using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class playerController : MonoBehaviour, IDamage
{
    public float moveSpeed;
    private Animator anim;
    private Vector2 moveDir;
    private Rigidbody2D rb;
    private SpriteRenderer sprite;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        Move();
        Animate();
    }

    private void Move()
    {
        float hor = Input.GetAxisRaw("Horizontal");
        float ver = Input.GetAxisRaw("Vertical");

        if (hor == 0 && ver == 0)
        {
            rb.linearVelocity = new Vector2 (0, 0);
            return;
        }

        if (hor != 0)
        {
            sprite.flipX = hor < 0;
        }

        moveDir = new Vector2(hor, ver);
        rb.linearVelocity = moveDir * moveSpeed * Time.deltaTime;
    }

    private void Animate()
    {
        if (anim)
        {
            anim.SetFloat("Movement X", moveDir.x);
            anim.SetFloat("Movement Y", moveDir.y);
        }
    }

    public void ApplyDamge(int amount)
    {
        throw new System.NotImplementedException();
    }
}
