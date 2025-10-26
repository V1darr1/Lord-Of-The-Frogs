using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FullScreenTestController : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float hurtDisplayTime = 0.15f;
    [SerializeField] private float hurtFadeOutTime = 0.35f;

    [Header("References")]
    [SerializeField] private ScriptableRendererFeature fullScreenDamage; // Full Screen Pass feature
    [SerializeField] private Material material;                          // material that uses your Shader Graph

    
    private static readonly int VignetteIntensityID = Shader.PropertyToID("_VignetteIntensity");
    private static readonly int VoronoiIntensityID = Shader.PropertyToID("_VoronoiIntensity");

    // starting values when the flash triggers
    [Header("Intensity")]
    [SerializeField] private float startVignette = 1.25f;
    [SerializeField] private float startVoronoi = 1.25f;

    void Start()
    {
        if (fullScreenDamage) fullScreenDamage.SetActive(false);
    }

    /// Call this when the player is hurt.
    public void TriggerHurt()
    {
        if (!isActiveAndEnabled) return;
        StopAllCoroutines();
        StartCoroutine(HurtRoutine());
    }

    private IEnumerator HurtRoutine()
    {
        if (fullScreenDamage) fullScreenDamage.SetActive(true);

        material.SetFloat(VignetteIntensityID, startVignette);
        material.SetFloat(VoronoiIntensityID, startVoronoi);

        // brief hold
        yield return new WaitForSeconds(hurtDisplayTime);

        // fade to zero
        float t = 0f;
        while (t < hurtFadeOutTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / hurtFadeOutTime);

            material.SetFloat(VignetteIntensityID, Mathf.Lerp(startVignette, 0f, k));
            material.SetFloat(VoronoiIntensityID, Mathf.Lerp(startVoronoi, 0f, k));

            yield return null;
        }

        if (fullScreenDamage) fullScreenDamage.SetActive(false);
    }

#if UNITY_EDITOR
    // optional test key in editor
    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
        {
            TriggerHurt();
        }
    }
#endif
}
