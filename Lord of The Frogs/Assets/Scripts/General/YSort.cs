using UnityEngine;
using UnityEngine.Rendering;

public class YSort : MonoBehaviour
{
    [SerializeField] int offset = 0;
    [SerializeField] int multiplier = 100;

    SpriteRenderer sr;
    SortingGroup sg;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>(); sg = GetComponent<SortingGroup>();
    }

    private void LateUpdate()
    {
        int order = -(int)(transform.position.y * multiplier) + offset;
        if (sg) sg.sortingOrder = order;
        else if (sr) sr.sortingOrder = order;
    }
}
