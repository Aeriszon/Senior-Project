using UnityEngine;

public class CarCollisionSystem : MonoBehaviour
{
    [Header("ค่าปรับแต่ละข้อหา")]
    public int fineBuilding = 5;  // ชนตึก
    public int fineCar = 10;      // ชนรถชาวบ้าน
    public int fineObstacle = 3;  // ชนกรวย/ขยะ

    // กันชนรัว (Cooldown)
    private float lastHitTime;
    public float hitCooldown = 2.0f; // เพิ่มเป็น 2 วิ เผื่อรถกระเด้งไปมา

    // ความแรงขั้นต่ำที่นับว่าชน (ป้องกันการจอดเบียดแล้วโดนปรับ)
    public float impactThreshold = 6.0f; 

    void OnCollisionEnter(Collision collision)
    {
        // 🛑 เพิ่มบรรทัดนี้เพื่อดูชื่อและ Tag ของสิ่งที่ชน:
        Debug.Log("💥 ชนโดน: " + collision.gameObject.name + " || Tag คือ: " + collision.gameObject.tag);
        Debug.Log("ความแรงที่ชนจริง: " + collision.relativeVelocity.magnitude);

        // 🛑 ยันต์กันผี: 5 วินาทีแรก ห้ามปรับเด็ดขาด! (ช่วงนับถอยหลัง)
        if (Time.timeSinceLevelLoad < 5.0f) return;
        
        // 1. เช็ค Cooldown
        if (Time.time < lastHitTime + hitCooldown) return;

        // 2. เช็คความแรง (ถ้าชนเบาๆ ไม่นับ)
        // ถ้าแรงไม่ถึง 6 (เช่นได้ 4-5) ก็ return ออกไปเลย ไม่โดนหัก
        if (collision.relativeVelocity.magnitude < impactThreshold) return;

        string tag = collision.gameObject.tag;
        bool hit = false;
        string msg = "";
        int fine = 0;

        // เช็ค Tag (ตัด Sidewalk ออก เพราะใช้ Trigger แยกต่างหากแล้ว)
        if (tag == "Building")
        {
            msg = "Building"; fine = fineBuilding; hit = true;
        }
        else if (tag == "NPC")
        {
            msg = "Car Crash"; fine = fineCar; hit = true;
        }
        else if (tag == "Obstacle")
        {
            msg = "Obstacle"; fine = fineObstacle; hit = true;
        }

        if (hit)
        {
            Debug.Log($"💥 ชน {tag} แรง {collision.relativeVelocity.magnitude}"); // ไว้ดู Log
            lastHitTime = Time.time; 

            // ✅✅ ไฮไลท์สำคัญ: เช็คก่อนว่า "ระบบยังเปิดอยู่ไหม" (Game Over หรือยัง?)
            if (TrafficLawManager.Instance != null && TrafficLawManager.Instance.isMonitoring)
            {
                // ถ้าเกมยังไม่จบ -> ค่อยสั่งบวกสถิติ (แยกตาม Tag ตรงนี้แทน)
                if (tag == "Building") TrafficLawManager.Instance.HitBuildingCount++;
                else if (tag == "NPC") TrafficLawManager.Instance.HitCarCount++;
                else if (tag == "Obstacle") TrafficLawManager.Instance.HitObstacleCount++;
            
                TrafficLawManager.Instance.ReportViolation(msg, fine);
            }
        }
    }
}