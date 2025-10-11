using UnityEngine;

[CreateAssetMenu(menuName = "Waves / Wave Definition")]
public class WaveDefinition : ScriptableObject
{
    [Header("What kinds of enemies appear at what ratios")]
    public WeightedEnemy[] enemies;

    [Header("How many to spawn this wave")]
    public int totalCount = 8;

    [Header("Spawn pacing")]
    public float spawnInterval = 0.3f;
}
