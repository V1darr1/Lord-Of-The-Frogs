using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class playerController : MonoBehaviour, IDamage
{
    [Header("Move")]
    [SerializeField] float moveSpeed = 5f;

    [Header("Melee (mouse-aimed cone)")]
    [SerializeField] float attackRange = 1.6f;
    [SerializeField] float attackArcDeg = 110f;
    [SerializeField] int attackDamage = 20;
    [SerializeField] LayerMask enemyMask;

    [Header("Optional")]
    [SerializeField] Transform attackOrigin;
    [SerializeField] bool rotateAttackOrigin = true;

    [Header("Combo")]
    [SerializeField] int maxCombo = 3;
    [SerializeField] float comboResetTime = 0.7f;
    [SerializeField] float inputBufferTime = 0.35f;
    [SerializeField] float postEndGrace = 0.18f;

    Rigidbody2D rb;
    Animator anim;
    health hp;
    Camera cam;

    bool facingRight = true;
    Vector2 moveDir;
    Vector2 faceDir = Vector2.right;

    int comboStep = 0;
    bool isAttacking = false;
    bool queued = false;
    float comboTimer = 0f;
    float bufferTimer = 0f;
    float lastClickTime = -999f;
    float lastClickedEndTime = -999f;

    [SerializeField] private AudioClip[] AttackSoundClips;
    [SerializeField] private AudioClip[] DeathSoundClip;

    // --- State and Persistence ---
    bool isDead = false;
    public static playerController Instance;


    void Awake()
    {

        if (Instance == null)
        {
            Instance = this;
            // Keep player alive between scenes
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Destroy the new instance if one already exists
            Destroy(gameObject);
        }
        // -------------------------

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        hp = GetComponent<health>();
        cam = Camera.main;

        if (hp)
        {
            hp.onDeath += OnDeath;
        }
    }


    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Main Menu")
        {
            // Reset the entire Animator state first
            if (anim)
            {
                anim.Rebind(); // Clears all internal state data
                anim.Update(0f); // Forces an immediate update
            }

            ResetStateForNewGame();
        }
    }


    public void ResetStateForNewGame()
    {
        // 1. Reset all state flags
        isDead = false;
        enabled = true;

        // 2. Clear Rigidbody velocity
        if (rb)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
        rb.velocity = Vector2.zero;
#endif
        }

        // 🛑 CRITICAL FIX: Reset the Animator and Health 🛑
        if (anim)
        {
            anim.SetBool("IsDead", false); // Assuming you use a bool for the dead state
            anim.SetTrigger("Respawn");    // Trigger transition back to Idle/Run
        }

        // 4. Heal the player to full HP (This MUST happen to clear the death state)
        if (hp)
        {
            hp.Heal(hp.MaxHP);
        }
    }


    void Update()
    {
        // Pause gate
        if (gameManager.instance != null && gameManager.instance.isPaused)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
            return;
        }

        // --- health gate ---
        if (isDead || (hp && !hp.isAlive))
        {
            // Keep frozen every frame so nothing else can nudge us
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
            moveDir = Vector2.zero;
            isAttacking = false;
            return;
        }

        UpdateAimFromMouse();
        HandleMove();
        HandleAttack();
        UpdateAnim();

        if (comboTimer > 0f) comboTimer -= Time.deltaTime;
        if (bufferTimer > 0f) bufferTimer -= Time.deltaTime;
        if (!isAttacking && comboTimer <= 0f && comboStep > 0) comboStep = 0;
    }

    // ---------- Mouse Aim ----------
    void UpdateAimFromMouse()
    {
        if (!cam) cam = Camera.main;
        Vector3 m = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 toMouse = (Vector2)m - (Vector2)transform.position;

        if (toMouse.sqrMagnitude > 0.0001f) faceDir = toMouse.normalized;

        if (faceDir.x > 0.02f && !facingRight) SetFacing(true);
        else if (faceDir.x < -0.02f && facingRight) SetFacing(false);

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
        if (isAttacking)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
            return;
        }

        float hor = Input.GetAxisRaw("Horizontal");
        float ver = Input.GetAxisRaw("Vertical");
        moveDir = new Vector2(hor, ver).normalized;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = moveDir * moveSpeed;
#else
        rb.velocity = moveDir * moveSpeed;
#endif
    }

    // ---------- Attack / Combo ----------
    void HandleAttack()
    {
        if (Input.GetMouseButtonDown(0))
        {
            lastClickTime = Time.time;
            comboTimer = comboResetTime;

            if (!isAttacking)
            {
                if (comboStep == 0 || Time.time - lastClickedEndTime <= postEndGrace)
                    comboStep = 1;
                PlayComboStep(comboStep);
            }
            else if (comboStep < maxCombo) queued = true;
        }
    }

    void PlayComboStep(int step)
    {
        if (isDead || (hp && !hp.isAlive)) return;

        isAttacking = true; queued = false; comboTimer = comboResetTime;

        switch (step)
        {
            case 1: anim.SetTrigger("Attack1"); break;
            case 2: anim.SetTrigger("Attack2"); break;
            case 3: anim.SetTrigger("Attack3"); break;
        }

        if (SoundFXManager.instance)
            // Assuming AttackSoundClips is correctly assigned
            SoundFXManager.instance.PlayRandomSoundFXClip(AttackSoundClips, transform, 1f);
    }

    public void Anim_Hit()
    {
        if (isDead || (hp && !hp.isAlive)) return;

        Vector2 origin = attackOrigin ? (Vector2)attackOrigin.position : (Vector2)transform.position;
        int mask = enemyMask.value == 0 ? ~0 : enemyMask.value;

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, attackRange, mask);
        if (hits == null || hits.Length == 0) return;

        float cosHalf = Mathf.Cos(0.5f * attackArcDeg * Mathf.Deg2Rad);
        bool isFinisher = (comboStep == 3);

        var processed = new HashSet<GameObject>();

        foreach (var h in hits)
        {
            if (!h) continue;
            GameObject enemy = h.gameObject;
            if (!processed.Add(enemy)) continue;

            Vector2 to = (Vector2)h.bounds.center - origin;
            float mag = to.magnitude; if (mag < 0.0001f) continue;
            Vector2 dir = to / mag;
            if (Vector2.Dot(dir, faceDir) < cosHalf) continue;


        }
    }

    public void Anim_QueueWindowOpen() { bufferTimer = inputBufferTime; }

    public void Anim_AttackEnd()
    {
        if (isDead || (hp && !hp.isAlive)) { isAttacking = false; return; }

        isAttacking = false; lastClickedEndTime = Time.time;

        if (queued && comboStep < maxCombo) { comboStep++; PlayComboStep(comboStep); return; }
        if (comboStep < maxCombo && Time.time - lastClickTime <= postEndGrace && comboTimer > 0f)
        { comboStep++; PlayComboStep(comboStep); return; }

        if (comboTimer > 0f || comboStep >= maxCombo) comboStep = 0;
    }

    // ---------- Taking damage ----------
    public void ApplyDamage(int amount)
    {
        if (isDead) return;
        if (hp) hp.ApplyDamage(amount);
    }

    void OnDeath()
    {
        if (isDead) return;
        MarkDeadAndFreeze();
    }

    // ---------- Unified death path ----------
    void MarkDeadAndFreeze()
    {
        if (isDead) return;
        isDead = true;

        if (SoundFXManager.instance)
            SoundFXManager.instance.PlayRandomSoundFXClip(DeathSoundClip, transform, 1f);

        // Keep velocity frozen
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector2.zero;
#else
    rb.velocity = Vector2.zero;
#endif
        moveDir = Vector2.zero;
        isAttacking = false;

        // Ensure the death animation plays
        if (anim)
        {

            anim.SetBool("Die", true);
            anim.SetTrigger("Die");

            anim.ResetTrigger("Attack1");
            anim.ResetTrigger("Attack2");
            anim.ResetTrigger("Attack3");

            // Final death trigger

        }
    }

    // ---------- Anim params ----------
    void UpdateAnim()
    {
        if (!anim) return;
        anim.SetFloat("Movement X", moveDir.x);
        anim.SetFloat("Movement Y", moveDir.y);
#if UNITY_6000_0_OR_NEWER
        anim.SetFloat("Speed", rb.linearVelocity.sqrMagnitude);
#else
        anim.SetFloat("Speed", rb.velocity.sqrMagnitude);
#endif
    }

    // ---------- Gizmos ----------
    void OnDrawGizmosSelected()
    {
        Vector2 origin = attackOrigin ? (Vector2)attackOrigin.position : (Vector2)transform.position;
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(origin, attackRange);

        Vector2 f = faceDir.sqrMagnitude < 0.001f ? Vector2.right : faceDir.normalized;
        float half = 0.5f * attackArcDeg * Mathf.Deg2Rad;
        Vector2 left = new Vector2(f.x * Mathf.Cos(half) - f.y * Mathf.Sin(half), f.x * Mathf.Sin(half) + f.y * Mathf.Cos(half));
        Vector2 right = new Vector2(f.x * Mathf.Cos(-half) - f.y * Mathf.Sin(-half), f.x * Mathf.Sin(-half) + f.y * Mathf.Cos(-half));
        Gizmos.DrawLine(origin, origin + left * attackRange);
        Gizmos.DrawLine(origin, origin + right * attackRange);
    }
}
