using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePortal : MonoBehaviour
{
    [SerializeField] string sceneToLoad;
    [SerializeField] DoorLock requiresUnlocked;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (requiresUnlocked && requiresUnlocked.IsLocked) return;

        if (string.IsNullOrWhiteSpace(sceneToLoad))
        {
            Debug.LogError($"[ScenePortal:{name}] Scene To Load is empty.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            Debug.LogError($"[ScenePortal:{name}] Scene '{sceneToLoad}' is not in Build Settings.");
            return;
        }

        SceneManager.LoadScene(sceneToLoad);
    }
}
