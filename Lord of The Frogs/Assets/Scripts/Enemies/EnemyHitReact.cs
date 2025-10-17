using System.Collections;
using UnityEngine;

[RequireComponent(typeof(enemyAI))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyHitReact : MonoBehaviour
{
    [Header("Stagger / Knockback")]
    [SerializeField] float staggerDuration = 0.25f;      // hits 1–2 freeze time
    [SerializeField] float finisherKB_Distance = 0.45f;  // hit 3 push distance
    [SerializeField] float finisherKB_Time = 0.06f;      // hit 3 push time

    [Header("Visuals")]
    [SerializeField] Transform visualRoot;                // sprite child
    [SerializeField] Color flashColor = new Color(1f, 0.5f, 0f);
    [SerializeField] float flashTime = 0.12f;
    [SerializeField] float shakeIntensity = 0.1f;
    [SerializeField] float shakeDuration = 0.15f;

    enemyAI ai;
    Rigidbody2D rb;
    SpriteRenderer sr;
    Color baseColor = Color.white;
    Vector3 visualBaseLocalPos;

    RigidbodyType2D savedBodyType;
    RigidbodyInterpolation2D savedInterp;
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

    // -------------------- hits 1–2: freeze + flash only --------------------
    public void ApplyStagger(float duration)
    {
        float d = duration > 0f ? duration : staggerDuration;

        if (ai) ai.Stagger(d);

        // temporarily set kinematic to stop any physics reaction
        savedBodyType = rb.bodyType;
        savedInterp = rb.interpolation;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.None;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector2.zero;
#else
        rb.velocity = Vector2.zero;
#endif
        rb.angularVelocity = 0f;

        // flash only (no shake)
        if (visualCo != null) StopCoroutine(visualCo);
        visualCo = StartCoroutine(CoFlashOnly());

        if (restoreCo != null) StopCoroutine(restoreCo);
        restoreCo = StartCoroutine(CoRestoreAfter(d));
    }

    IEnumerator CoRestoreAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        rb.bodyType = savedBodyType;
        rb.interpolation = savedInterp;
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector2.zero;
#else
        rb.velocity = Vector2.zero;
#endif
        rb.angularVelocity = 0f;
    }

    // -------------------- hit 3: knockback + flash + shake --------------------
    public void ApplyKnockbackFromPosition(Vector2 attackerPos)
    {
        if (ai) ai.Stagger(finisherKB_Time);

        // make sure we’re dynamic again
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (visualCo != null) StopCoroutine(visualCo);
        visualCo = StartCoroutine(CoFlashAndShake());
        StartCoroutine(CoKnockback(attackerPos));
    }

    IEnumerator CoKnockback(Vector2 attackerPos)
    {
        Vector2 dir = ((Vector2)transform.position - attackerPos).normalized;
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

    // -------------------- visual helpers --------------------
    IEnumerator CoFlashOnly()
    {
        if (!sr) yield break;
        sr.color = flashColor;
        yield return new WaitForSeconds(flashTime);
        sr.color = baseColor;
    }

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
}
