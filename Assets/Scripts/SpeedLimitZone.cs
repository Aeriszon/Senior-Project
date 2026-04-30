using UnityEngine;

public class SpeedLimitZone : MonoBehaviour
{
    [Header("ตั้งค่าโซน")]
    [Tooltip("ความเร็วสูงสุดที่อนุญาต (km/h)")]
    public float maxSpeed = 60f; // ค่าเริ่มต้น (ถนนปกติ)

    [Tooltip("ชื่อโซน (เอาไว้โชว์ตอนเข้าโซน)")]
    public string zoneName = "Normal Road"; 

    // วาดกล่องสีเขียวจางๆ ในหน้า Scene จะได้วางง่ายๆ
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        if (GetComponent<BoxCollider>())
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(GetComponent<BoxCollider>().center, GetComponent<BoxCollider>().size);
        }
    }
}
