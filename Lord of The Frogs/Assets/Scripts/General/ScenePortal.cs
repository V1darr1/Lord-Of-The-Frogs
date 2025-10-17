using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePortal : MonoBehaviour
{
    [SerializeField] string sceneToLoad;
    [SerializeField] string spawnId = "default";
    [SerializeField] float enterCooldown = 0.1f;
    public static string NextSpawnId;
    float enabledAt;

    private void OnEnable() => enabledAt = Time.unscaledTime;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var doorLock = GetComponent<DoorLock>() ?? GetComponentInParent<DoorLock>();
        if (doorLock && doorLock.IsLocked)
        {
            var rb = other.attachedRigidbody;
            if (rb) rb.linearVelocity = Vector2.zero;
            return;
        }
        if (Time.unscaledTime - enabledAt < enterCooldown)
            return;

        NextSpawnId = spawnId;
        SceneManager.LoadScene(sceneToLoad);
    }
}
