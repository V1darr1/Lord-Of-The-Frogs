using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSpawnManager : MonoBehaviour
{
    [Tooltip("Used if NextSpawnId is null or no matching spawn is found")]
    public string defaultSpawnId = "G1";

    private void OnEnable() => SceneManager.sceneLoaded += OnLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnLoaded;

    private void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return;

        var spawns = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);
        if (spawns.Length == 0) return;

        // 1) Which spawn do we want?
        string desiredId = !string.IsNullOrEmpty(ScenePortal.NextSpawnId)
            ? ScenePortal.NextSpawnId
            : defaultSpawnId;

        // 2) Try to find an exact match; else fallback to default; else first
        Transform spawn = null;
        foreach (var s in spawns) if (s && s.id == desiredId) { spawn = s.transform; break; }
        if (!spawn && !string.IsNullOrEmpty(defaultSpawnId))
            foreach (var s in spawns) if (s && s.id == defaultSpawnId) { spawn = s.transform; break; }
        if (!spawn) spawn = spawns[0].transform;

        // 3) Move player exactly to spawn (preserve z for 2D)
        var p = player.transform.position;
        player.transform.position = new Vector3(spawn.position.x, spawn.position.y, p.z);

        // 4) Clear for next load
        ScenePortal.NextSpawnId = null;

        // 5) Zero momentum so we don't slide off the point
        var rb2d = player.GetComponent<Rigidbody2D>();
        if (rb2d) rb2d.linearVelocity = Vector2.zero;
    }
}