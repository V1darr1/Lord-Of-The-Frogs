using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class EnemyArea : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] LayerMask enemyMask;
    [SerializeField] bool findAtStart = true;
    [SerializeField] bool includeChildrenOfEnemies = true;

    [Header("Doors to control")]
    [SerializeField] DoorLock[] doorsToLock;

    public UnityEvent OnAreaCleared;

    readonly List<health> tracked = new();
    int alive;

    Collider2D trigger;

    private void Awake()
    {
        trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }

    private void Start()
    {
        foreach (var d in doorsToLock) if (d) d.Lock();

        if (findAtStart)
            ScanAndRegisterEnemies();

        Debug.Log($"[EnemyArea] Registered {tracked.Count} enemies in {name}");
    }

    public void RegisterEnemy(health hp)
    {
        if (!hp || tracked.Contains(hp)) return;
        tracked.Add(hp);
        alive++;
        hp.onDeath += () => OnEnemyDied(hp);
    }

    void OnEnemyDied(health hp)
    {
        if (!tracked.Contains(hp)) return;
        tracked.Remove(hp);
        alive = Mathf.Max(0, alive - 1);

        if (alive == 0)
        {
            foreach (var d in doorsToLock) if (d) d.Unlock();
            OnAreaCleared?.Invoke();
        }
    }

    public void ScanAndRegisterEnemies()
    {
        alive = 0;
        tracked.Clear();

        var results = new List<Collider2D>(64);
        var filter = new ContactFilter2D { useLayerMask = true, layerMask = enemyMask, useTriggers = true };
        trigger.Overlap(filter, results);

        foreach (var col in results)
        {
            if (!col) continue;
            health hp = includeChildrenOfEnemies ? col.GetComponentInParent<health>() : col.GetComponent<health>();
            if (hp && hp.isAlive) RegisterEnemy(hp);
        }
    }

    private void OnDrawGizmos()
    {
        var col = GetComponent<Collider2D>();
        if (!col) return;
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);

        if (col is BoxCollider2D b)
            Gizmos.DrawCube((Vector2)transform.position + b.offset, b.size);
        else if (col is CircleCollider2D c)
            Gizmos.DrawWireSphere((Vector2)transform.position + c.offset, c.radius);
    }
}
