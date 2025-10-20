using System.Collections;
using UnityEngine;

public class BackgroundBossController : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] string playerTag = "Player";
    [SerializeField] LayerMask playerMask;

    [Header("Firing")]
    [SerializeField] BossWaveProjectile2D projectilePrefab;
    [SerializeField] Transform muzzle;
    [SerializeField] float projectileSpeed = 11f;
    [SerializeField] int projectileDamage = 2;
    [SerializeField] float fireInterval = 1.75f;
    [SerializeField] Vector2 burstCountRange = new Vector2(1, 1);
    [SerializeField] float burstSpacing = 0.18f;

    [Header("Telegraph")]
    [SerializeField] float telegraphTime = 0.35f;
    [SerializeField] AudioClip telegraphSfx;
    [SerializeField] AudioClip fireSfx;

    [Header("Inaccuracy (Fairness)")]
    [SerializeField] Vector2 errorDegreesMinMax = new Vector2(4f, 12f);
    [SerializeField] float errorPerUnitDistance = 0.7f;
    [SerializeField] float minAbsoluteError = 2.5f;
    [SerializeField] float maxAbsoluteError = 22f;

    [Header("Misc")]
    [SerializeField] float startTimeJitter = 0.6f;
    [SerializeField] float deadZoneRadius = 0f;

    Transform player;
    Coroutine loop;
    System.Random rng;

    public System.Action onFire;
    private void Awake()
    {
        if (!muzzle) muzzle = transform;
        rng = new System.Random();
    }

    private void Start()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) player = p.transform;

        float delay = fireInterval * 0.5f + Random.Range(0f, startTimeJitter);
        loop = StartCoroutine(FireLoop(delay));
    }

    private void OnDisable()
    {
        if (loop != null) StopCoroutine(loop);
    }

    IEnumerator FireLoop(float initialDelay)
    {
        if (initialDelay > 0f) yield return new WaitForSeconds(initialDelay);

        while(true)
        {
            if (!player || !projectilePrefab) { yield return null; continue; }

            int shots = Mathf.RoundToInt(Random.Range(burstCountRange.x, burstCountRange.y + 0.49f));
            shots = Mathf.Max(1, shots);

            for (int i = 0; i < shots; i++)
            {
                if (telegraphTime > 0f)
                {
                    if (telegraphSfx && SoundFXManager.instance)
                        SoundFXManager.instance.PlaySoundFXClip(telegraphSfx, transform, 1f);
                    yield return new WaitForSeconds(telegraphTime);
                }

                Vector2 origin = muzzle ? (Vector2)muzzle.position : (Vector2)transform.position;
                Vector2 toPlayer = (player ? (Vector2)player.position : origin) - origin;
                float dist = toPlayer.magnitude;

                if (deadZoneRadius > 0f && dist < deadZoneRadius)
                {
                    yield return new WaitForSeconds(burstSpacing);
                    continue;
                }

                Vector2 dir = (dist > 0.001f) ? toPlayer / dist : Vector2.right;

                float baseErr = Random.Range(errorDegreesMinMax.x, errorDegreesMinMax.y);
                float distErr = dist * errorPerUnitDistance;
                float totalErr = Mathf.Clamp(baseErr + distErr, minAbsoluteError, maxAbsoluteError);

                float signed = (float)((rng.NextDouble() * 2.0 - 1.0)) * totalErr;
                if (Mathf.Abs(signed) < minAbsoluteError) signed = Mathf.Sign(signed == 0 ? 1f : signed) * minAbsoluteError;

                float rad = signed * Mathf.Deg2Rad;
                Vector2 aim = new Vector2(
                    dir.x * Mathf.Cos(rad) - dir.y * Mathf.Sin(rad),
                    dir.x * Mathf.Sin(rad) + dir.y * Mathf.Cos(rad)
                ).normalized;

                if (fireSfx && SoundFXManager.instance)
                    SoundFXManager.instance.PlaySoundFXClip(fireSfx, transform, 1f);

                onFire?.Invoke();

                var proj = Instantiate(projectilePrefab, origin, Quaternion.identity);
                proj.Init(aim * projectileSpeed, projectileDamage, playerMask);

                if (i < shots - 1) yield return new WaitForSeconds(burstSpacing);
            }

            yield return new WaitForSeconds(fireInterval);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!muzzle) muzzle = transform;
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(muzzle.position, 0.2f);

        Vector2 forward = Vector2.right;
        float halfA = Mathf.Max(minAbsoluteError, errorDegreesMinMax.x) * Mathf.Deg2Rad;
        Vector2 left = new Vector2(Mathf.Cos(halfA), Mathf.Sin(halfA));
        Vector2 right = new Vector2(Mathf.Cos(halfA), -Mathf.Sin(halfA));
        float r = 2.2f;
        Gizmos.DrawLine(muzzle.position, (Vector2)muzzle.position + left * r);
        Gizmos.DrawLine(muzzle.position, (Vector2)muzzle.position + right * r);
    }
}