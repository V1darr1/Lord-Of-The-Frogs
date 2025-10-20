using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// When Large_Map_1 loads after New Game, briefly locks player input
/// and shows an optional overlay, then releases control. Does not pause time.
/// </summary>
public class LargeMapSpawnGate : MonoBehaviour
{
    [Header("UI (optional)")]
    [SerializeField] GameObject overlayPanel;     // Canvas/Panel (can be null)
    [SerializeField] float fadeIn = 0.15f;        // seconds
    [SerializeField] float holdTime = 1.0f;       // seconds
    [SerializeField] float fadeOut = 0.15f;       // seconds

    [Header("Optional behaviours to disable by class name (no hard refs)")]
    [Tooltip("Names of components to temporarily disable, if present on the Player.")]
    [SerializeField]
    string[] optionalBehaviourNames =
    {
        "DashSkill",
        "MeleeSwing",
        "SwordSpinSkill"
    };

    void Start()
    {
        if (!GateFlags.PlayLargeMapIntroGate) return; // only after New Game
        StartCoroutine(RunGate());
    }

    IEnumerator RunGate()
    {
        GateFlags.PlayLargeMapIntroGate = false;  // consume one-shot

        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player) yield break;

        var pc = player.GetComponent<playerController>();
        var rb = player.GetComponent<Rigidbody2D>();
        var hp = player.GetComponent<health>();

        var optionals = player.GetComponentsInChildren<Behaviour>(includeInactive: true)
            .Where(b => b != null && optionalBehaviourNames.Contains(b.GetType().Name))
            .ToArray();

        // ---- Overlay on ----
        if (overlayPanel)
        {
            overlayPanel.SetActive(true);
            var cg = overlayPanel.GetComponent<CanvasGroup>();
            if (cg && fadeIn > 0f)
            {
                cg.alpha = 0f;
                float t = 0f;
                while (t < fadeIn)
                {
                    t += Time.unscaledDeltaTime;
                    cg.alpha = t / fadeIn;
                    yield return null;
                }
                cg.alpha = 1f;
            }
        }

        // ---- Lock controls (do NOT pause world) ----
        bool wasPCEnabled = pc && pc.enabled;
        bool wasHPEnabled = hp && hp.enabled;

        if (pc) pc.enabled = false;
        foreach (var b in optionals) b.enabled = false;

        if (rb)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
            rb.angularVelocity = 0f;
        }

        if (hp) hp.enabled = false;

        // Wait in real time
        float timer = 0f;
        while (timer < holdTime)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        // ---- Release lock ----
        if (pc) pc.enabled = wasPCEnabled;
        foreach (var b in optionals) if (b) b.enabled = true;
        if (hp) hp.enabled = wasHPEnabled;

        // ---- Overlay off ----
        if (overlayPanel)
        {
            var cg = overlayPanel.GetComponent<CanvasGroup>();
            if (cg && fadeOut > 0f)
            {
                float t = 0f;
                while (t < fadeOut)
                {
                    t += Time.unscaledDeltaTime;
                    cg.alpha = 1f - (t / fadeOut);
                    yield return null;
                }
            }
            overlayPanel.SetActive(false);
        }
    }
}
