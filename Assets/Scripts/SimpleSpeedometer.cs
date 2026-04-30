using UnityEngine;
using TMPro; // อย่าลืมบรรทัดนี้เพื่อใช้ TextMeshPro

public class SimpleSpeedometer : MonoBehaviour
{
    public Rigidbody playerCarRb;       // ลากรถ Player มาใส่ช่องนี้
    public TextMeshProUGUI speedText;   // ลากข้อความ UI มาใส่ช่องนี้

    void Update()
    {
        if (playerCarRb != null && speedText != null)
        {
            // Unity 6 ต้องใช้ linearVelocity แทน velocity (คำสั่งเก่า obsolete แล้ว)
            // คูณ 3.6 เพื่อแปลง m/s เป็น km/h
            float speedKmh = playerCarRb.linearVelocity.magnitude * 3.6f;

            // แสดงผลแบบไม่มีทศนิยม (Format "0")
            speedText.text = speedKmh.ToString("0");
        }
    }
}
