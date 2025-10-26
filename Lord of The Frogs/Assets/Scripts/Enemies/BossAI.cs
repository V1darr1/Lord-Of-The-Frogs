using System.Collections;
using UnityEngine;

/// <summary>
/// Simple boss animation driver that plays Idle by default and triggers Attack
/// each time the BackgroundBossController fires a wave.
/// </summary>
[RequireComponent(typeof(Animator))]
public class BossAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] BackgroundBossController bossController;
    [SerializeField] Animator anim;

    [Header("Animation Parameters")]
    [Tooltip("Trigger name in Animator for attack animation.")]
    [SerializeField] string attackTrigger = "Attack";
    [Tooltip("Trigger or state name for idle animation.")]
    [SerializeField] string idleStateName = "Idle";
    [Tooltip("Delay (seconds) to return to idle after attack.")]
    [SerializeField] float idleDelay = 0.5f;

    bool isAttacking;

    void Reset()
    {
        anim = GetComponent<Animator>();
        bossController = GetComponent<BackgroundBossController>();
    }

    void Awake()
    {
        if (!anim) anim = GetComponent<Animator>();
        if (bossController)
            bossController.onFire += OnBossAttack; // hook into the firing event
    }

    void OnDestroy()
    {
        if (bossController)
            bossController.onFire -= OnBossAttack;
    }

    void OnBossAttack()
    {
        if (isAttacking || !anim) return;
        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        anim.SetTrigger(attackTrigger);
        yield return new WaitForSeconds(idleDelay);
        anim.Play(idleStateName);
        isAttacking = false;
    }
}

