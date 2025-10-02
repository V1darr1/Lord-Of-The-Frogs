using UnityEngine;

public class DoTZone2D : MonoBehaviour
{
    [SerializeField] int dps = 1;
    [SerializeField] float tick = 0.5f;
    [SerializeField] float lifetime = 4f;

    float timer;

    void Start() { Destroy(gameObject, lifetime); }

    private void OnTriggerStay2D(Collider2D other)
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = tick;

        var hp = other.GetComponent<health>();
        if (hp) hp.ApplyDamge(Mathf.RoundToInt(dps * tick));
    }
}
