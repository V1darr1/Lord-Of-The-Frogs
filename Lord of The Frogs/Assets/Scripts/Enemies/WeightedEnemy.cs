using UnityEngine;

[System.Serializable]
public class WeightedEnemy
{
    public GameObject prefab;
    [Min(0f)] public float weight = 1f;
}
