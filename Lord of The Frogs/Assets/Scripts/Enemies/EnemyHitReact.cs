using System.Collections;
using UnityEngine;

[RequireComponent(typeof(enemyAI))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyHitReact : MonoBehaviour
{
    [Header("Stagger / Knockback")]
    [SerializeField] float staggerDuration = 0.25f;   // hits 1–2 freeze time
    [SerializeField] float finisherKB_Distance = 0.45f;
    [SerializeField] float finisherKB_Time = 0.06f;

    [Header("Visuals (cosmetic only)")]
    [SerializeField] Transform visualRoot;            // set to sprite child
    [SerializeField] Color flashColor = new Color(1f, 0.5f, 0f);
    [SerializeField] float flashTime = 0.12f;
    [SerializeField] float shakeIntensity = 0.1f;
    [SerializeField] float shakeDuration = 0.15f;

    enemyAI ai;
    Rigidbody2D rb;
    SpriteRenderer sr;
    Color baseColor;
    Vector3 visualBaseLocalPos;

    RigidbodyConstraints2D savedConstraints;
    RigidbodyInterpolation2D savedInterpolation;
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
    }

    // ------------------- HITS 1–2: freeze completely -------------------
    public void ApplyStagger(float duration)
    {
        float d = duration > 0 ? duration : staggerDuration;

        // Tell the AI to pause logic
        if (ai) ai.Stagger(d);

        // Save physics settings and freeze
        savedConstraints = rb.constraints;
        savedInterpolation = rb.interpolation;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector2.zero;
#else
        rb.velocity = Vector2.zero;
#endif
        rb.angularVelocity = 0f;

        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.constraints = RigidbodyConstraints2D.FreezePositionX |
                         RigidbodyConstraints2D.FreezePositionY |
                         RigidbodyConstraints2D.FreezeRotation;

        // Flash and shake
        if (visualCo != null) StopCoroutine(visualCo);
        visualCo = StartCoroutine(CoFlashAndShake());

        if (restoreCo != null) StopCoroutine(restoreCo);
        restoreCo = StartCoroutine(CoRestoreAfter(d));
    }

    IEnumerator CoRestoreAfter(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Restore physics exactly as before
        rb.constraints = savedConstraints;
        rb.interpolation = savedInterpolation;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector2.zero;
#else
        rb.velocity = Vector2.zero;
#endif
        rb.angularVelocity = 0f;
    }

    // ------------------- HIT 3: small, fixed shove -------------------
    public void ApplyKnockbackFromPosition(Vector2 attackerPos)
    {
        // Ensure unfrozen, brief-stun AI
        rb.constraints = savedConstraints;
        rb.interpolation = savedInterpolation;
        if (ai) ai.Stagger(finisherKB_Time);

        if (visualCo != null) StopCoroutine(visualCo);
        visualCo = StartCoroutine(CoFlashAndShake());

        StartCoroutine(CoFixedKnockback(attackerPos));
    }

    IEnumerator CoFixedKnockback(Vector2 attackerPos)
    {
        Vector2 dir = ((Vector2)transform.position - attackerPos);
        dir = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector2.right;

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
            float e = 1f - (1f - a) * (1f - a); // ease-out
            rb.MovePosition(Vector2.Lerp(start, end, e));
            yield return wait;
        }
        rb.MovePosition(end);
    }

    // ------------------- Flash + Shake -------------------
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
            visualRoot.localPosition = visualBaseLocalPos +
                                       (Vector3)(Random.insideUnitCircle * shakeIntensity);
            yield return null;
        }
        visualRoot.localPosition = visualBaseLocalPos;
    }

#if UNITY_EDITOR
    // Red ring when frozen (Scene view only)
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || rb == null) return;
        bool frozen = (rb.constraints &
                      (RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY)) != 0;
        if (frozen)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
        }
    }
#endif
}
