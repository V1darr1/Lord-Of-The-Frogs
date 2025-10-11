using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class WaveRoomController : MonoBehaviour
{
    [Header("Config")]
    public WaveDefinition[] waves;
    public Transform[] spawnPoints;

    [Header("Flow")]
    public float delayBeforeFirstWave = 1f;
    public float delayBetweenWaves = 2f;

    public UnityEvent OnRoomStarted;
    public UnityEvent OnRoomCleared;

    int currentWave = -1;
    int alive = 0;
    bool running;

    public void StartRoom()
    {
        if (running) return;
        running = true;
        OnRoomStarted?.Invoke();
        StartCoroutine(RunWaves());
    }

    IEnumerator RunWaves()
    {
        yield return new WaitForSeconds(delayBeforeFirstWave);

        for (int w = 0; w < waves.Length; w++)
        {
            currentWave = w;
            yield return StartCoroutine(SpawnWave(waves[w]));
            // wait until wave is cleared
            while (alive > 0) yield return null;
            yield return new WaitForSeconds(delayBetweenWaves);
        }

        running = false;
        OnRoomCleared?.Invoke();
    }

    IEnumerator SpawnWave(WaveDefinition def)
    {
        if (def == null || def.enemies == null || def.enemies.Length == 0 || def.totalCount <= 0)
            yield break;

        // build cumulative weights for efficient sampling
        float totalW = 0f;
        for (int i = 0; i < def.enemies.Length; i++) totalW += Mathf.Max(0f, def.enemies[i].weight);

        for (int i = 0; i < def.totalCount; i++)
        {
            var spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];

            // pick enemy by weight
            float r = Random.value * totalW;
            int chosen = 0;
            float acc = 0f;
            for (int k = 0; k < def.enemies.Length; k++)
            {
                acc += Mathf.Max(0f, def.enemies[k].weight);
                if (r <= acc) { chosen = k; break; }
            }

            var prefab = def.enemies[chosen].prefab;
            if (prefab)
            {
                var inst = Instantiate(prefab, spawn.position, Quaternion.identity);

                // subscribe to death to track progress
                var hp = inst.GetComponentInChildren<health>();
                if (hp != null && hp.isAlive)
                {
                    alive++;
                    hp.onDeath += () => { alive = Mathf.Max(0, alive - 1); };
                }
            }

            if (def.spawnInterval > 0f)
                yield return new WaitForSeconds(def.spawnInterval);
            else
                yield return null;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (spawnPoints == null) return;
        Gizmos.color = Color.green;
        foreach (var t in spawnPoints)
            if (t) Gizmos.DrawWireSphere(t.position, 0.25f);
    }
#endif
}
