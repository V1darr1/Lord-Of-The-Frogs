using UnityEngine;

public class DamageNumberSpawner : MonoBehaviour
{
    public static DamageNumberSpawner Instance;

    public GameObject damageNumberPrefab;

    void Awake()
    {
        Instance = this;
    }

    public void Spawn(Vector3 position, int amount, bool crit = false)
    {
        if (!damageNumberPrefab) return;

        var go = Instantiate(damageNumberPrefab, position, Quaternion.identity);
        var dn = go.GetComponent<DamageNumber>();
        if (dn) dn.Setup(amount, crit);
    }
}
