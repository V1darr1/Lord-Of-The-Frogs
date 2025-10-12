using UnityEngine;

public class SceneMusicGuard : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource; // on this GO
    [SerializeField] private bool playOnStart = true;

    void Awake()
    {
        if (!musicSource) musicSource = GetComponent<AudioSource>();
        // If another identical music source is already playing this clip, destroy this one
        var all = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (var a in all)
        {
            if (a == musicSource) continue;
            if (a.isPlaying && a.clip == musicSource.clip)
            {
                Destroy(gameObject);
                return;
            }
        }
    }

    void Start()
    {
        if (playOnStart && musicSource && !musicSource.isPlaying)
            musicSource.Play();
    }
}
