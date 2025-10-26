using UnityEngine;
using System.Collections;

/// <summary>
/// Trigger zone that smoothly zooms the main camera (attached to the player)
/// out when entering a boss area and restores it when exiting.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CameraZoomTrigger : MonoBehaviour
{
    [Header("Zoom Settings")]
    [Tooltip("Desired zoom level when inside this trigger.")]
    [SerializeField] private float targetZoom = 7f;
    [Tooltip("How fast the zoom transitions (units per second).")]
    [SerializeField] private float zoomSpeed = 2f;
    [Tooltip("Wait this long before zooming back after leaving (optional).")]
    [SerializeField] private float exitDelay = 0.5f;

    private Camera mainCam;
    private float defaultZoom;
    private Coroutine zoomRoutine;

    void Awake()
    {
        var camObj = Camera.main;
        if (camObj) mainCam = camObj;
    }

    void Start()
    {
        if (mainCam)
            defaultZoom = mainCam.orthographicSize;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || !mainCam) return;

        if (zoomRoutine != null) StopCoroutine(zoomRoutine);
        zoomRoutine = StartCoroutine(SmoothZoom(targetZoom));
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || !mainCam) return;

        if (zoomRoutine != null) StopCoroutine(zoomRoutine);
        zoomRoutine = StartCoroutine(ResetZoomAfterDelay());
    }

    IEnumerator SmoothZoom(float newSize)
    {
        while (Mathf.Abs(mainCam.orthographicSize - newSize) > 0.05f)
        {
            mainCam.orthographicSize = Mathf.MoveTowards(
                mainCam.orthographicSize, newSize, zoomSpeed * Time.deltaTime);
            yield return null;
        }
        mainCam.orthographicSize = newSize;
    }

    IEnumerator ResetZoomAfterDelay()
    {
        if (exitDelay > 0f) yield return new WaitForSeconds(exitDelay);
        yield return SmoothZoom(defaultZoom);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
        var c = GetComponent<Collider2D>();
        if (c) Gizmos.DrawCube(c.bounds.center, c.bounds.size);
    }
}
