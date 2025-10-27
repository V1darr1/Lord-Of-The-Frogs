using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class playerController : MonoBehaviour, IDamage
{
    private bool inputEnabled = true;
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

    [Header("Audio")]
    [SerializeField] AudioClip[] AttackSoundClips;
    [SerializeField] AudioClip[] DeathSoundClip;

    [Header("Persistence")]
    [Tooltip("If true, a single Player persists across scenes. Extra instances auto-destroy.")]
    [SerializeField] bool persistAcrossScenes = true;
    [Tooltip("If true, this script revives the player automatically on scene load (except Main Menu).")]
    [SerializeField] bool autoReviveOnSceneLoad = true;

    // --- Components ---
    Rigidbody2D rb;
    Animator anim;
    health hp;
    Camera cam;

    // --- Facing / Move state ---
    bool facingRight = true;
    Vector2 moveDir;
    Vector2 faceDir = Vector2.right;

    // --- Combo state ---
    int comboStep = 0;
    bool isAttacking = false;
    bool queued = false;
    float comboTimer = 0f;
    float bufferTimer = 0f;
    float lastClickTime = -999f;
    float lastClickedEndTime = -999f;

    // --- Death gate ---
    bool isDead = false;

    // --- Singleton (optional) ---
    public static playerController Instance;

    // ---------- Lifecycle ----------
    void Awake()
    {
        // Optional singleton/persistence
        if (persistAcrossScenes)
        {
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else if (Instance != this) { Destroy(gameObject); return; }
        }

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        hp = GetComponent<health>();
        cam = Camera.main;

        if (hp) hp.onDeath += OnDeath;

        // Use Normal update while alive
        if (anim) anim.updateMode = AnimatorUpdateMode.Normal;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (hp) hp.onDeath -= OnDeath;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!autoReviveOnSceneLoad) return;
        if (scene.name == "Main Menu") return;
        // Rebind animator to clear any sub-state garbage, then revive to full control.
        if (anim) { anim.Rebind(); anim.Update(0f); }
        ForceReviveForRestart();
    }
    public void SetInputEnabled(bool state)
    {
        inputEnabled = state;

        if (!state && rb)
        {

#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
        rb.velocity = Vector2.zero;
#endif
        }
    }
    // ---------- Update ----------
    void Update()
    {

        if (!inputEnabled)
        {
            if (rb)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
            }
            return; // Stops all movement and actions instantly
        }

        // If HP just hit 0 and we haven't processed death yet, process now
        if (!isDead && hp && !hp.isAlive) MarkDeadAndFreeze();

        // Pause gate
        if (gameManager.instance != null && gameManager.instance.isPaused)
        {
            // Player input is already blocked by the main gate, but we ensure freeze here.
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
        rb.velocity = Vector2.zero;
#endif
            return;
        }

        // Dead = hard freeze (This should ideally not be reached if MarkDeadAndFreeze() is working)
        if (isDead || (hp && !hp.isAlive))
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
        rb.velocity = Vector2.zero;
#endif
            moveDir = Vector2.zero;
            isAttacking = false;
            return;
        }

        // Alive loop
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
            SoundFXManager.instance.PlayRandomSoundFXClip(AttackSoundClips, transform, 1f);
    }

    // Called from animation event
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

            // Damage + reaction
            DamageInvoker.ApplyHit(enemy, attackDamage, origin, 0f);
            var react = enemy.GetComponent<EnemyHitReact>();
            if (react)
            {
                if (!isFinisher) react.ApplyStagger(0.25f);
                else react.ApplyKnockbackFromPosition(origin);
            }
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

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector2.zero;
#else
        rb.velocity = Vector2.zero;
#endif
        moveDir = Vector2.zero;
        isAttacking = false;

        if (anim)
        {
            // Ensure death anim plays even if the game is paused this frame
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;
            anim.SetBool("Die", true);       // if controller uses a bool
            anim.ResetTrigger("Attack1");
            anim.ResetTrigger("Attack2");
            anim.ResetTrigger("Attack3");
            anim.SetTrigger("Die");          // if controller uses a trigger
        }
        // Keep enabled so Update continues to freeze motion.
    }

    // ---------- Revival API (for restarts / scene loads) ----------
    public void ForceReviveForRestart()
    {
        isDead = false;
        isAttacking = false;
        moveDir = Vector2.zero;

#if UNITY_6000_0_OR_NEWER
        if (rb) rb.linearVelocity = Vector2.zero;
#else
        if (rb) rb.velocity = Vector2.zero;
#endif
        if (rb) rb.angularVelocity = 0f;

        if (anim)
        {
            anim.updateMode = AnimatorUpdateMode.Normal;
            anim.ResetTrigger("Die");
            anim.SetBool("Die", false);
            anim.ResetTrigger("Attack1");
            anim.ResetTrigger("Attack2");
            anim.ResetTrigger("Attack3");
        }

        // Heal to full if your health exposes MaxHP / Heal
        if (hp)
        {
            hp.SendMessage("FullHeal", SendMessageOptions.DontRequireReceiver);
            hp.SendMessage("HealToFull", SendMessageOptions.DontRequireReceiver);
            hp.SendMessage("Revive", SendMessageOptions.DontRequireReceiver);
            hp.SendMessage("ReviveToFull", SendMessageOptions.DontRequireReceiver);
            hp.Heal(hp.MaxHP);
        }

        enabled = true;

        // In case any colliders were disabled on death
        var cols = GetComponentsInChildren<Collider2D>(includeInactive: false);
        foreach (var c in cols) if (c && !c.enabled) c.enabled = true;
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

    public void ResetStateForNewGame()
    {
        // Make sure we have refs (covers domain reloads)
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!anim) anim = GetComponent<Animator>();
        if (!hp) hp = GetComponent<health>();

        // 1) Clear internal state
        isDead = false;
        isAttacking = false;
        queued = false;
        comboStep = 0;
        comboTimer = 0f;
        bufferTimer = 0f;
        moveDir = Vector2.zero;
        faceDir = Vector2.right;
        enabled = true;

        // 2) Zero physics
#if UNITY_6000_0_OR_NEWER
        if (rb) rb.linearVelocity = Vector2.zero;
#else
    if (rb) rb.velocity = Vector2.zero;
#endif
        if (rb) rb.angularVelocity = 0f;

        // 3) Reset animator cleanly
        if (anim)
        {
            anim.updateMode = AnimatorUpdateMode.Normal; // back to normal time
            anim.Rebind();            // wipe cached state
            anim.Update(0f);          // force immediate eval

            // clear death/attack params used by our controller
            anim.ResetTrigger("Die");
            anim.SetBool("Die", false);
            anim.ResetTrigger("Attack1");
            anim.ResetTrigger("Attack2");
            anim.ResetTrigger("Attack3");
        }

        // 4) Restore health to full (use whatever your health exposes)
        if (hp)
        {
            // prefer concrete API if present
            hp.SendMessage("FullHeal", SendMessageOptions.DontRequireReceiver);
            hp.SendMessage("HealToFull", SendMessageOptions.DontRequireReceiver);
            hp.SendMessage("Revive", SendMessageOptions.DontRequireReceiver);
            hp.SendMessage("ReviveToFull", SendMessageOptions.DontRequireReceiver);
            // fallback if you have Heal(int) & MaxHP
            hp.Heal(hp.MaxHP);
        }

        // 5) Ensure colliders re-enabled (if death disabled them)
        var cols = GetComponentsInChildren<Collider2D>(includeInactive: false);
        foreach (var c in cols) if (c && !c.enabled) c.enabled = true;
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
