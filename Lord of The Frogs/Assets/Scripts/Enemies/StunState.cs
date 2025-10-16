using UnityEngine;

public class StunState : MonoBehaviour
{
    public bool IsStunned { get; private set; }
    float _until;

    /// <summary>Apply (or extend) stun for duration seconds.</summary>
    public void Stun(float duration)
    {
        if (duration <= 0f) return;
        IsStunned = true;
        _until = Mathf.Max(_until, Time.time + duration);
    }

    void Update()
    {
        if (IsStunned && Time.time >= _until)
            IsStunned = false;
    }
}
