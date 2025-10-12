using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SwordSpinSkill : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode spinKey = KeyCode.Space;

    [Header("Spin Tuning")]
    [SerializeField] private float spinDuration = 0.5f;
    [SerializeField] private float tickRate = 0.1f;   // how often we apply damage during the spin
    [SerializeField] private float cooldown = 2.0f;

    [Header("Damage")]
    [SerializeField] private int damagePerTick = 6;
    [SerializeField] private float knockback = 4f;
    [SerializeField] private float radius = 1.8f;
    [SerializeField] private LayerMask enemyMask;

    [Header("Invulnerability (optional)")]
    [SerializeField] private bool grantIFrames = false;
    [SerializeField] private string playerLayerName = "Player";
    [SerializeField] private string enemyLayerName = "Enemy";

    [Header("VFX/Audio (optional)")]
    [SerializeField] private AudioClip spinSfx;
    [SerializeField] private GameObject spinVfxPrefab;

    private float readyAt;
    private bool spinning;

    private Behaviour cachedController;

    void Awake()
    {
        cachedController = FindPlayerControllerOnThisObject();
    }

    void Update()
    {
        if (spinning) return;

        if (Input.GetKeyDown(spinKey) && Time.time >= readyAt)
        {
            StartCoroutine(DoSpin());
            readyAt = Time.time + cooldown;
        }
    }

    IEnumerator DoSpin()
    {
        spinning = true;

        // audio
        if (spinSfx && SoundFXManager.instance)
            SoundFXManager.instance.PlaySoundFXClip(spinSfx, transform, 1f);

        // vfx
        GameObject vfx = null;
        if (spinVfxPrefab) vfx = Instantiate(spinVfxPrefab, transform.position, Quaternion.identity, transform);

        // disable movement while spinning (classic locked spin)
        SetControllerEnabled(false);

        // optional i-frames
        int pLayer = LayerMask.NameToLayer(playerLayerName);
        int eLayer = LayerMask.NameToLayer(enemyLayerName);
        bool toggleLayers = grantIFrames && pLayer >= 0 && eLayer >= 0;
        if (toggleLayers) Physics2D.IgnoreLayerCollision(pLayer, eLayer, true);

        float elapsed = 0f;
        float nextTick = 0f;

        while (elapsed < spinDuration)
        {
            // keep VFX centered
            if (vfx) vfx.transform.position = transform.position;

            if (elapsed >= nextTick)
            {
                DoDamageTick();
                nextTick += Mathf.Max(0.01f, tickRate);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (toggleLayers) Physics2D.IgnoreLayerCollision(pLayer, eLayer, false);

        if (vfx) Destroy(vfx);
        SetControllerEnabled(true);
        spinning = false;
    }

    void DoDamageTick()
    {
        Vector2 origin = transform.position;
        var hits = Physics2D.OverlapCircleAll(origin, radius, enemyMask);

        // prevent double-hitting the same target in the same tick
        var done = new HashSet<GameObject>();
        foreach (var h in hits)
        {
            if (!h) continue;
            var go = h.attachedRigidbody ? h.attachedRigidbody.gameObject : h.gameObject;
            if (done.Contains(go)) continue;
            done.Add(go);

            DamageInvoker.ApplyHit(go, damagePerTick, origin, knockback);
        }
    }

    Behaviour FindPlayerControllerOnThisObject()
    {
        var all = GetComponents<Behaviour>();
        foreach (var b in all)
        {
            if (!b) continue;
            string n = b.GetType().Name;
            if (n == "playerController" || n == "PlayerController")
                return b;
        }
        return null;
    }

    void SetControllerEnabled(bool v)
    {
        if (cachedController) cachedController.enabled = v;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
