using UnityEngine;
using TMPro; 

public class ArrowPointer : MonoBehaviour
{
    public CheckpointRandomizer gameManager; 
    public Transform playerCar;              
    public TextMeshProUGUI distanceText;     

    public float turnSpeed = 10f; 
    public Vector3 offset = new Vector3(0, 2f, 0); 

    void Update()
    {
        // 1. เช็คชีพจร: สคริปต์ทำงานไหม?
        // (ถ้านายไม่เห็นข้อความอะไรเลยใน Console แปลว่าสคริปต์นี้ถูกปิดอยู่!)

        if (gameManager == null) { Debug.LogError("❌ Arrow: ไม่มี Game Manager!"); return; }
        if (playerCar == null) { Debug.LogError("❌ Arrow: ไม่มี Player Car!"); return; }

        // 2. บังคับให้ตามรถ (ถ้ารถขยับ ลูกศรต้องขยับตาม)
        transform.position = playerCar.position + offset;

        // 3. ถามหาเป้าหมาย
        Transform target = gameManager.GetCurrentTarget();

        if (target != null)
        {
            // เจอเป้าหมาย!
            //Debug.Log($"✅ Arrow: กำลังชี้ไปที่ {target.name}"); // <--- ดูบรรทัดนี้

            Vector3 direction = target.position - transform.position;
            direction.y = 0; 
            if (direction != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * turnSpeed);
            }

            float dist = Vector3.Distance(playerCar.position, target.position);
            if (distanceText != null) distanceText.text = dist.ToString("F0") + " m";
        }
        else
        {
            // ไม่เจอเป้าหมาย!
            Debug.LogWarning("⚠️ Arrow: Game Manager บอกว่า 'Target เป็น Null' (หา Checkpoint ไม่เจอ)");
            if(distanceText != null) distanceText.text = "Searching...";
        }
    }
}