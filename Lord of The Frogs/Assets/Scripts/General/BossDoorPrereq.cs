using UnityEngine;

public class BossDoorPrereq : MonoBehaviour
{
    [SerializeField] DoorLock bossDoor;
    [SerializeField] EnemyArea[] prerequisites;

    int completed;

    private void Awake()
    {
        if (!bossDoor) bossDoor = GetComponent<DoorLock>();
    }

    private void Start()
    {
        if (bossDoor) bossDoor.Lock();

        completed = 0;
        foreach (var area in prerequisites)
        {
            if (!area) continue;
            area.OnAreaCleared.AddListener(OnPrereqCleared);
        }
    }

    void OnPrereqCleared()
    {
        completed++;
        if (completed >=  prerequisites.Length)
        {
            bossDoor.Unlock();
        }
    }
}
