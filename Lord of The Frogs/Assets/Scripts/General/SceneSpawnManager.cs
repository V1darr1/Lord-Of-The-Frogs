using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSpawnManager : MonoBehaviour
{
    public string defaultSpawnId = "G1"; // fallback

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnLoaded;
    }

    private void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return;

        var spawns = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);
        if (spawns.Length == 0) return;

        var spawn = spawns[0].transform;
        player.transform.position = spawn.position;
    }
}
