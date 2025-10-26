using System.Collections;
using UnityEngine;

public class HurtFlashOnMainCamera : MonoBehaviour
{
    [Header("Material (the SAME one set in your Full Screen Pass)")]
    [SerializeField] private Material material;

    [Header("Timings")]
    [SerializeField] private float holdTime = 0.15f;
    [SerializeField] private float fadeTime = 0.35f;

    [Header("Start Intensities")]
    [SerializeField] private float startVignette = 1.25f;
    [SerializeField] private float startVoronoi = 1.25f;

    // Match Shader Graph **Reference** names (underscored):
    static readonly int VignetteIntensityID = Shader.PropertyToID("_VignetteIntensity");
    static readonly int VoronoiIntensityID = Shader.PropertyToID("_VoronoiIntensity");

    /// Call this to play the hurt flash.
    public void TriggerHurt()
    {
        if (!material) { Debug.LogWarning("HurtFlashOnMainCamera: Material not assigned."); return; }
        StopAllCoroutines();
        StartCoroutine(HurtRoutine());
    }

    IEnumerator HurtRoutine()
    {
        // kick values up
        material.SetFloat(VignetteIntensityID, startVignette);
        material.SetFloat(VoronoiIntensityID, startVoronoi);

        // brief hold
        yield return new WaitForSeconds(holdTime);

        // fade down
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeTime);
            material.SetFloat(VignetteIntensityID, Mathf.Lerp(startVignette, 0f, k));
            material.SetFloat(VoronoiIntensityID, Mathf.Lerp(startVoronoi, 0f, k));
            yield return null;
        }
    }

#if UNITY_EDITOR
    void Update()
    {
        // Quick test: press E to trigger
        if (Input.GetKeyDown(KeyCode.E)) TriggerHurt();
    }
#endif
}
