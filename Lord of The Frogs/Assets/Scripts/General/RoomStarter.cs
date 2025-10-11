using UnityEngine;

public class RoomStarter : MonoBehaviour
{
    public WaveRoomController controller;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) { controller.StartRoom(); Destroy(gameObject); }
    }
}
