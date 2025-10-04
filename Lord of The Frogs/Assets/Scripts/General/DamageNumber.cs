using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
    public float floatSpeed = 1f;
    public float lifetime = 1f;
    public Color normalColor = Color.yellow;
    public Color critColor = Color.red;

    private TextMeshPro textMesh;
    private float timer;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    public void Setup(int amount, bool crit = false)
    {
        if (!textMesh) textMesh = GetComponent<TextMeshPro>();

        textMesh.text = amount.ToString();
        textMesh.color = crit ? critColor : normalColor;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Float upward
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        // Fade out
        if (textMesh)
        {
            float alpha = Mathf.Lerp(1f, 0f, timer / lifetime);
            textMesh.color = new Color(textMesh.color.r, textMesh.color.g, textMesh.color.b, alpha);
        }

        if (timer >= lifetime)
            Destroy(gameObject);
    }
}
