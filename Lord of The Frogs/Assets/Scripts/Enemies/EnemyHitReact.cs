using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(enemyAI))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyHitReact : MonoBehaviour
{
    [Header("Stagger / Knockback")]
    [SerializeField] float staggerDuration = 0.25f;      // hits 1–2
    [SerializeField] float finisherKB_Distance = 0.45f;  // hit 3
    [SerializeField] float finisherKB_Time = 0.06f;

    [Header("Visuals (cosmetic only)")]
    [SerializeField] Transform visualRoot;                // sprite child for shake; if null auto-pick
    [SerializeField] Color flashColor = new Color(1f, 0.5f, 0f);
    [SerializeField] float flashTime = 0.12f;
    [SerializeField] float shakeIntensity = 0.1f;
    [SerializeField] float shakeDuration = 0.15f;

    enemyAI ai;
    Rigidbody2D rb;
    SpriteRenderer sr;
    Color baseColor = Color.white;
    Vector3 visualBaseLocalPos;

    // saved physics state
    RigidbodyType2D savedBodyType;
    RigidbodyInterpolation2D savedInterp;

    // colliders to toggle ignores
    Collider2D[] myCols;
    static Collider2D[] cachedPlayerCols;

    Coroutine visualCo;
    Coroutine restoreCo;

    void Awake()
    {
        ai = GetComponent<enemyAI>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        if (!visualRoot) visualRoot = sr ? sr.transform : transform;
        if (sr) baseColor = sr.color;
        visualBaseLocalPos = visualRoot.localPosition;

        myCols = GetComponentsInChildren<Collider2D>(includeInactive: false);
        savedBodyType = rb.bodyType;
        savedInterp = rb.interpolation;
    }

    // ---------- HITS 1–2: freeze with kinematic + ignore collisions ----------
    public void ApplyStagger(float duration)
    {
        float d = (duration > 0f) ? duration : staggerDuration;

        // freeze AI logic timers
        if (ai) ai.Stagger(d);

        // save & set to kinematic so physics can't push it
        savedBodyType = rb.bodyType;
        savedInterp = rb.interpolation;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector2.zero;
#else
        rb.velocity = Vector2.zero;
#endif
        rb.angularVelocity = 0f;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.None;

        // temporarily ignore collisions with player to avoid separation impulses
        TogglePlayerCollision(ignore: true);

        // visuals
        if (visualCo != null) StopCoroutine(visualCo);
        visualCo = StartCoroutine(CoFlashAndShake());

        if (restoreCo != null) StopCoroutine(restoreCo);
        restoreCo = StartCoroutine(CoRestoreAfter(d));
    }

    IEnumerator CoRestoreAfter(float delay)
    {
        yield return new WaitForSeconds(delay);

        // restore physics state
        rb.bodyType = savedBodyType;
        rb.interpolation = savedInterp;
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector2.zero;
#else
        rb.velocity = Vector2.zero;
#endif
        rb.angularVelocity = 0f;

        TogglePlayerCollision(ignore: false);
    }

    // ---------- HIT 3: small, fixed shove ----------
    public void ApplyKnockbackFromPosition(Vector2 attackerPos)
    {
        // ensure not kinematic so MovePosition works as expected on a Dynamic body
        rb.bodyType = savedBodyType == 0 ? RigidbodyType2D.Dynamic : savedBodyType;
        rb.interpolation = savedInterp;

        if (ai) ai.Stagger(finisherKB_Time);

        if (visualCo != null) StopCoroutine(visualCo);
        visualCo = StartCoroutine(CoFlashAndShake());

        StartCoroutine(CoFixedKnockback(attackerPos));
    }

    IEnumerator CoFixedKnockback(Vector2 attackerPos)
    {
        Vector2 dir = (Vector2)transform.position - attackerPos;
        dir = (dir.sqrMagnitude > 1e-6f) ? dir.normalized : Vector2.right;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector2.zero;
#else
        rb.velocity = Vector2.zero;
#endif
        rb.angularVelocity = 0f;

        Vector2 start = rb.position;
        Vector2 end = start + dir * finisherKB_Distance;

        float t = 0f;
        var wait = new WaitForFixedUpdate();
        while (t < finisherKB_Time)
        {
            t += Time.fixedDeltaTime;
            float a = Mathf.Clamp01(t / finisherKB_Time);
            float e = 1f - (1f - a) * (1f - a);
            rb.MovePosition(Vector2.Lerp(start, end, e));
            yield return wait;
        }
        rb.MovePosition(end);
    }

    // ---------- visuals ----------
    IEnumerator CoFlashAndShake()
    {
        if (sr)
        {
            sr.color = flashColor;
            yield return new WaitForSeconds(flashTime);
            sr.color = baseColor;
        }

        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            visualRoot.localPosition = visualBaseLocalPos + (Vector3)(Random.insideUnitCircle * shakeIntensity);
            yield return null;
        }
        visualRoot.localPosition = visualBaseLocalPos;
    }

    // cache player colliders and toggle ignore
    void TogglePlayerCollision(bool ignore)
    {
        if (cachedPlayerCols == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) cachedPlayerCols = p.GetComponentsInChildren<Collider2D>(includeInactive: false);
        }
        if (cachedPlayerCols == null || myCols == null) return;

        foreach (var ec in myCols)
            if (ec && ec.enabled)
                foreach (var pc in cachedPlayerCols)
                    if (pc && pc.enabled)
                        Physics2D.IgnoreCollision(ec, pc, ignore);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (rb && rb.bodyType == RigidbodyType2D.Kinematic)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
        }
    }
#endif
}
