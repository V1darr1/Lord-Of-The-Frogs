using System.Collections;
using UnityEngine;

[RequireComponent (typeof(health))]
public class EnemyHitReact : MonoBehaviour
{
    [Header("Targets")]
    public SpriteRenderer[] sprites;

    [Header("Visuals")]
    public Color flashColor = Color.white;
    public float flashTime = 0.07f;
    public float shakeTime = 0.10f;
    public float shakeMagnitude = 0.06f;

    [Header("Control")]
    public float staggerTime = 0.15f;

    health hp;
    enemyAI ai;
    Rigidbody rb;
    Vector3 baseLocalPos;
    float lastHP;
    bool shaking;

    private void Awake()
    {
        hp = GetComponent<health>();
        ai = GetComponent<enemyAI>();
        rb = GetComponent<Rigidbody>();

        if (sprites == null || sprites.Length == 0 )
            sprites = GetComponentsInChildren<SpriteRenderer>();

        baseLocalPos = transform.localPosition;
        lastHP = hp.CurrentHP;

        hp.onDeath += () => { transform.localPosition = baseLocalPos; };
    }

    private void Update()
    {
        if (hp.CurrentHP < lastHP)
        {
            OnDamaged(lastHP - hp.CurrentHP);
            lastHP = hp.CurrentHP;
        }
        else if (hp.CurrentHP > lastHP)
        {
            lastHP = hp.CurrentHP;
        }
    }

    void OnDamaged(float delta)
    {
        if (ai) ai.Stagger(staggerTime);

        if (rb) rb.linearVelocity = Vector2.zero;

        Flash();
        if (!shaking) StartCoroutine(ShakeCo());
    }

    void Flash()
    {
        if (sprites == null) return;
        foreach (var s in sprites)
            if (s) s.material.SetColor("_Color", flashColor);

        Invoke(nameof(RestoreColors), flashTime);
    }

    void RestoreColors()
    {
        if (sprites == null) return;
        foreach (var s in sprites)
            if (s) s.material.SetColor("_Color", Color.white);
    }

    IEnumerator ShakeCo()
    {
        shaking = true;
        float t = 0f;
        while (t < shakeTime)
        {
            t += Time.deltaTime;
            float damper = 1f - (t / shakeTime);
            Vector2 j = Random.insideUnitCircle * shakeMagnitude * damper;
            transform.localPosition = baseLocalPos + (Vector3)j;
            yield return null;
        }
        transform.localPosition = baseLocalPos;
        shaking = false;
    }
}
