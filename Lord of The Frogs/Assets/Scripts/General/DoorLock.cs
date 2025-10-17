using UnityEngine;
using UnityEngine.Events;

public class DoorLock : MonoBehaviour
{
    [SerializeField] Collider2D blocker;
    [SerializeField] Animator anim; // SET UP DOOR LOCK ANIMATION SOON
    [SerializeField] bool lockOnStart = true;

    public bool IsLocked { get; private set; }

    public UnityEvent onLocked;
    public UnityEvent onUnlocked;

    private void Reset()
    {
        if (!blocker) blocker = GetComponent<Collider2D>();
        if (!anim) anim = GetComponent<Animator>();
    }

    void Awake()
    {
        if (!blocker) blocker = GetComponent<Collider2D>();
        if (!anim) anim = GetComponent<Animator>();

        if (lockOnStart) Lock();
        else Unlock();
    }

    public void Lock()
    {
        IsLocked = true;
        if (blocker) blocker.enabled = true;
        if (anim) anim.SetBool("Locked", true);
        onLocked?.Invoke();
    }

    public void Unlock()
    {
        IsLocked = false;
        if (anim) anim.SetBool("Locked", false);
        if (blocker) blocker.enabled = false;
        onUnlocked?.Invoke();
    }
}
