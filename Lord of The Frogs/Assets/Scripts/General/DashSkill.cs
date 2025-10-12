using UnityEngine;
using System.Collections;

public class DashSkill2D : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;
    [SerializeField] private int mouseButton = 1; // 1 = RMB; set to -1 to disable mouse

    [Header("Direction")]
    [SerializeField] private bool towardsMouse = true;  // if false, uses current rigidbody velocity / input dir
    [SerializeField] private Vector2 fallbackDir = Vector2.right; // used if no input and not towardsMouse

    [Header("Dash Tuning")]
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashTime = 0.12f;
    [SerializeField] private float cooldown = 0.8f;

    [Header("Invulnerability (optional)")]
    [SerializeField] private bool grantIFrames = true;
    [SerializeField] private string playerLayerName = "Player";
    [SerializeField] private string enemyLayerName = "Enemy";

    [Header("Audio (optional)")]
    [SerializeField] private AudioClip dashSfx;

    private Rigidbody2D rb;
    private float readyAt;
    private bool dashing;

    // cache refs to whichever controller script you use
    private Behaviour cachedController;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cachedController = FindPlayerControllerOnThisObject();
    }

    void Update()
    {
        if (dashing) return;

        bool pressedKey = Input.GetKeyDown(dashKey);
        bool pressedMouse = (mouseButton >= 0) && Input.GetMouseButtonDown(mouseButton);

        if ((pressedKey || pressedMouse) && Time.time >= readyAt)
        {
            Vector2 dir = ComputeDashDirection();
            StartCoroutine(DoDash(dir.normalized));
            readyAt = Time.time + cooldown;
        }
    }

    Vector2 ComputeDashDirection()
    {
        if (towardsMouse)
        {
            Vector3 m = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dir = ((Vector2)m - (Vector2)transform.position);
            if (dir.sqrMagnitude < 0.0001f) dir = fallbackDir;
            return dir;
        }
        // otherwise, try current rb velocity
        Vector2 v = rb ? rb.linearVelocity : Vector2.zero;
        if (v.sqrMagnitude > 0.0001f) return v;
        return fallbackDir;
    }

    IEnumerator DoDash(Vector2 dir)
    {
        dashing = true;

        // sound
        if (dashSfx && SoundFXManager.instance)
            SoundFXManager.instance.PlaySoundFXClip(dashSfx, transform, 1f);

        // disable controller so it won't overwrite velocity
        SetControllerEnabled(false);

        // i-frames by ignoring collision
        int pLayer = LayerMask.NameToLayer(playerLayerName);
        int eLayer = LayerMask.NameToLayer(enemyLayerName);
        bool toggleLayers = grantIFrames && pLayer >= 0 && eLayer >= 0;

        if (toggleLayers) Physics2D.IgnoreLayerCollision(pLayer, eLayer, true);

        float t = dashTime;
        while (t > 0f)
        {
            if (rb) rb.linearVelocity = dir * dashSpeed; // Unity 6 "linearVelocity"
            t -= Time.deltaTime;
            yield return null;
        }

        if (rb) rb.linearVelocity = Vector2.zero;

        if (toggleLayers) Physics2D.IgnoreLayerCollision(pLayer, eLayer, false);

        // small safety delay then re-enable controller
        yield return null;
        SetControllerEnabled(true);
        dashing = false;
    }

    Behaviour FindPlayerControllerOnThisObject()
    {
        // Works whether your class is "playerController" or "PlayerController"
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
}
