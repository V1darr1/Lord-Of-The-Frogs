using UnityEngine;
using TMPro;
using UnityEngine.Windows;

public class playerFlask : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] int maxFlasks = 3;
    [SerializeField] int healAmount = 40;
    [SerializeField] KeyCode healKey = KeyCode.R;
    [SerializeField] float healCooldown;

    [Header("UI")]
    [SerializeField] private TMP_Text flaskText;

    [Header("Sound")]
    [SerializeField] AudioClip healSound;

    int currentFlasks;
    float cooldownTimer = 0f;
    health hp;
    AudioSource audioSource;

    private void Awake()
    {
        hp = GetComponent<health>();
        audioSource = GetComponent<AudioSource>();
        currentFlasks = maxFlasks;
        UpdateUI();
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (UnityEngine.Input.GetKeyDown(healKey))
            TryHeal();
    }

    void TryHeal()
    {
        if (cooldownTimer > 0f) return;
        if (currentFlasks <= 0) return;
        if (!hp || !hp.isAlive) return;
        if (hp.CurrentHP >= hp.MaxHP) return;

        hp.Heal(healAmount);
        currentFlasks--;
        cooldownTimer = healCooldown;

        if (healSound && audioSource)
            audioSource.PlayOneShot(healSound);

        UpdateUI();
    }

    void UpdateUI()
    {
        if (flaskText)
            flaskText.text = $"{currentFlasks}/{maxFlasks}";
    }

    public void RefillFlask()
    {
        currentFlasks = maxFlasks;
        UpdateUI();
    }

    public int GetFlaskCount() => currentFlasks;
    public void SetFlaskCount(int count)
    {
        currentFlasks = Mathf.Clamp(count, 0, maxFlasks);
        UpdateUI();
    }
}
