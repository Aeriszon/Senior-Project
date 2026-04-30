using UnityEngine;

public class TrafficViolationTrigger : MonoBehaviour
{
    public enum ViolationType { NoEntry, SpeedLimit, RedLight, Sidewalk }

    [Header("Settings")]
    public ViolationType violationType;
    public int fineAmount = 500;
    
    [Header("Options")]
    public float speedLimit = 120f;   
    public bool isOneTimeOnly = false; 

    // ✅ 1. เพิ่มบรรทัดนี้: เอาไว้ลาก Object หลอดไฟแดงมาใส่
    public GameObject redLightObject;

    // ✅ เพิ่มตรงนี้: ตัวปรับเวลาอนุโลม (Grace Period)
    [Tooltip("เวลาที่อนุญาตให้เฉี่ยวได้ก่อนโดนปรับ (วินาที)")]
    public float gracePeriod = 0.5f;

    // ✅ เพิ่ม 2 บรรทัดนี้ครับ (ตัวแปรส่วนกลาง จำเวลาล่าสุดที่โดนจับไฟแดง)
    private static float lastRedLightFineTime = -999f; 
    private const float violationCooldown = 3.0f; // เวลาพักยก 3 วินาที

    // Cooldown ใช้ร่วมกัน (Static) สำหรับ Sidewalk
    private static float lastSidewalkFineTime = -999f; 
    private const float sidewalkCooldown = 3.0f; // โดนหักทุกๆ 3 วินาทีถ้าแช่

    private bool hasTriggered = false; 

    // เพิ่มตัวแปรส่วนตัว เพื่อจำเวลาที่ "เริ่มเหยียบ" ของแต่ละกล่อง
    private float enterTime = 0f;
    private bool isChecking = false;

    // 1. เข้าปุ๊บ ยังไม่ปรับ! แค่จำเวลาไว้
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            enterTime = Time.time; // จดเวลาที่เริ่มแตะขอบ
            isChecking = true;     // เริ่มจับตาดู

            // ✅ เรียกฟังก์ชันเดียวพอ ไม่ต้องเช็คเงื่อนไขตรงนี้ (ไปเช็คข้างในเอา)
            CheckAndFine(other);
        }
    }

    // 2. ถ้าออกก่อนกำหนด -> ยกเลิกการจับผิด
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isChecking = false; // รอดตัวไป!
        }
    }

    // 3. ระหว่างที่แช่อยู่ ค่อยเช็คเวลา
    void OnTriggerStay(Collider other)
    {
        // ✅ เพิ่มบรรทัดนี้ครอบไว้เลยครับ: ถ้าไม่ใช่ทางเท้า "ถีบออกไปเลย" ห้ามทำงานต่อ
        if (violationType != ViolationType.Sidewalk) return;

        // --- Logic เฉพาะทางเท้า ---
        if (isChecking && other.CompareTag("Player"))
        {
            // เช็คว่าเหยียบมานานเกิน 0.5 วินาที หรือยัง? (ปรับเวลาตรงนี้ได้)
            if (Time.time >= enterTime + 0.5f) 
            {
                CheckAndFine(other); // นานเกินไปแล้ว... ปรับ!
            }
        }
    }

    // ฟังก์ชันตรวจสอบและสั่งปรับ (รวมไว้ที่เดียวจะได้ไม่เขียนซ้ำ)
    void CheckAndFine(Collider other)
    {
        if (Time.timeSinceLevelLoad < 6.0f) return;
        if (isOneTimeOnly && hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            Rigidbody rb = other.GetComponentInParent<Rigidbody>();

            if (violationType == ViolationType.NoEntry)
            {
                Fine("No Entry");
            }

            // ✅ 2. แทรกท่อนนี้เข้าไปครับ (เช็คว่าไฟเปิดอยู่ไหม)
            else if (violationType == ViolationType.RedLight)
            {
                // 1. เช็คคูลดาวน์ (เหมือนเดิม)
                if (Time.time < lastRedLightFineTime + 3.0f) return;

                // ✅✅ 2. เพิ่มท่อนนี้ครับ: ถ้ากำลังย้อนศรอยู่ ถือว่าไฟแดงไม่มีผล (ให้ไปโดนข้อหาย้อนศรแทน)
                if (TrafficLawManager.Instance != null && TrafficLawManager.Instance.isWrongWayActive)
                {
                    // Debug.Log("🛡️ รอดไฟแดง เพราะย้อนศรอยู่ (ไปโดนปรับย้อนศรนู่น)");
                    return; 
                }

                // ✅✅ 3. (เพิ่มใหม่) เช็คทิศทางรถ: ถ้าขับ "ตัดหน้า" หรือ "สวนทาง" Trigger นี้ = ไม่นับ
                // transform.forward = ทิศของ Trigger
                // other.transform.forward = ทิศของรถ
                // ถ้าค่า Dot > 0.5 แสดงว่าหันหน้าไปทางเดียวกัน (เตรียมฝ่าไฟแดง) -> ถึงจะโดน
                // ถ้าค่า Dot < 0.5 (เช่น 0 คือตั้งฉาก/เลี้ยว) -> รอด
                float directionCheck = Vector3.Dot(transform.forward, other.transform.forward);
                if (directionCheck < 0.5f) 
                {
                    // Debug.Log("รอด! เพราะรถกำลังเลี้ยวตัดหน้า Trigger ไม่ได้จะฝ่าตรงๆ");
                    return;
                }

                // 4. เช็คไฟแดง (เหมือนเดิม)
                if (IsRedLightActive())
                {
                    // ✅ แก้เป็นแบบนี้: เช็คก่อนว่าระบบยังเปิดอยู่ไหม ค่อยนับ
                    if (TrafficLawManager.Instance != null && TrafficLawManager.Instance.isMonitoring)
                    {
                        TrafficLawManager.Instance.RedLightCount++;
                    }

                    Fine("RED LIGHT");
                    lastRedLightFineTime = Time.time; 
                }
            }

            else if (violationType == ViolationType.Sidewalk)
            {
                // เช็คว่าหมดเวลาพักยกหรือยัง?
                if (Time.time >= lastSidewalkFineTime + sidewalkCooldown)
                {
                    // ✅ เพิ่ม If ครอบไว้: "ถ้าปิดระบบแล้ว (จบเกม) ไม่ต้องนับ!"
                    if (TrafficLawManager.Instance != null && TrafficLawManager.Instance.isMonitoring) 
                    {
                        TrafficLawManager.Instance.SidewalkCount++; // ย้ายมาอยู่ในนี้
                        Fine("Sidewalk");
                        lastSidewalkFineTime = Time.time; 
                    }
                }
            }
            else if (violationType == ViolationType.SpeedLimit)
            {
                float speed = (rb != null) ? rb.linearVelocity.magnitude * 3.6f : 0;
                if (speed > speedLimit) Fine($"Speed Limit {speed:F0}");
            }
        }
    }

    void Fine(string msg)
    {
        if (TrafficLawManager.Instance != null)
        {
            TrafficLawManager.Instance.ReportViolation(msg, fineAmount);
            hasTriggered = true;
        }
    }

    // ✅ เพิ่มฟังก์ชันนี้เข้าไปครับ (วางไว้ล่างสุดของไฟล์ ก่อนปีกกาปิด } ตัวสุดท้าย)
    bool IsRedLightActive()
    {
        // ถ้ามีของใส่มา และของชิ้นนั้นเปิดอยู่ (Active) = แดงจริง
        return redLightObject != null && redLightObject.activeInHierarchy;
    }
}