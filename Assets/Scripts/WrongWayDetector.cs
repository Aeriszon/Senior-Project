using UnityEngine;
using Gley.TrafficSystem;

public class WrongWayDetector : MonoBehaviour
{
    [Header("⚙️ Settings")]
    public float timeToConfirm = 2.0f;
    public float penaltyTime = 3.0f;
    public float allowedAngle = 130f;   // เพิ่มมุมให้กว้างขึ้นอีกนิด (เผื่อถนนโค้ง)
    public float minSpeedToCheck = 10f;
    
    [Header("🎯 Sensitivity")]
    public float maxLaneDistance = 4.0f; // ⚠️ สำคัญ: ถ้าห่างจากเส้นถนนเกิน 4 เมตร ถือว่าหาไม่เจอ (จะได้ไม่จับมั่ว)

    [Header("Penalty")]
    public int fineAmount = 5;

    private float wrongWayTimer = 0f;
    private Transform carTransform;
    private Rigidbody carRb;
    private bool isWarningShown = false;

    void Start()
    {
        carTransform = transform;
        carRb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Debug: ลากเส้นสีเขียวหน้ารถ
        Debug.DrawRay(carTransform.position, carTransform.forward * 3, Color.green);

        if (TrafficLawManager.Instance == null || !TrafficLawManager.Instance.isMonitoring) 
        {
            ResetWarning();
            return;
        }

        float speed = (carRb != null) ? carRb.linearVelocity.magnitude * 3.6f : 0f;
        if (speed < minSpeedToCheck) 
        {
            ResetWarning();
            return;
        }

        CheckWrongWayLogic();
    }

    void CheckWrongWayLogic()
    {
        // 1. หา Waypoint ที่ใกล้ที่สุด
        var waypoint = API.GetClosestWaypoint(carTransform.position);

        if (waypoint != null)
        {
            // 📏 เช็คระยะห่าง: ถ้ารถเราอยู่ไกลจากจุด Waypoint เกิน 4 เมตร...
            // แสดงว่าเราอาจจะขับ "คร่อมเลน" หรือ "ลงข้างทาง" -> อย่าเพิ่งตัดสิน!
            float dist = Vector3.Distance(carTransform.position, waypoint.Position);
            
            // 🛠️ DEBUG: ลากเส้นสีแดงไปหาถนนที่มันเจอ (จะได้รู้ว่ามันจับเส้นไหน)
            Debug.DrawLine(carTransform.position, waypoint.Position, Color.red);

            if (dist > maxLaneDistance)
            {
                // ไกลเกินไป ไม่ชัวร์ อย่าจับ
                ResetWarning();
                return;
            }

            // 📐 เช็คทิศทาง
            bool isWrongWay = false;
            if (waypoint.Angle != null && waypoint.Angle.Length > 0)
            {
                float roadRotation = waypoint.Angle[0];
                Vector3 roadDirection = Quaternion.Euler(0, roadRotation, 0) * Vector3.forward;

                // Debug: ลากเส้นสีเหลืองบอกทิศถนน
                Debug.DrawRay(waypoint.Position, roadDirection * 3, Color.yellow);

                float angle = Vector3.Angle(carTransform.forward, roadDirection);

                if (angle > allowedAngle)
                {
                    isWrongWay = true;
                }
            }

            // --- Logic ลงโทษ ---
            if (isWrongWay)
            {
                wrongWayTimer += Time.deltaTime;

                // ช่วงเตือน (Warning)
                if (wrongWayTimer > timeToConfirm && wrongWayTimer < penaltyTime)
                {
                    if (!isWarningShown)
                    {
                        TrafficLawManager.Instance.ShowWarning("WRONG WAY?", 3.0f);
                        isWarningShown = true;
                    }
                }
                // ช่วงลงโทษ (Penalty)
                else if (wrongWayTimer >= penaltyTime)
                {
                    Debug.Log("🚨 WRONG WAY PENALTY!");
                    TrafficLawManager.Instance.ReportViolation("WRONG WAY", fineAmount);
                    wrongWayTimer = 1.0f; // รีเซ็ตนิดหน่อย ให้โอกาสหนี
                    isWarningShown = false;
                }
            }
            else
            {
                ResetWarning();
            }
        }
        else
        {
            ResetWarning();
        }
    }

    void ResetWarning()
    {
        if (wrongWayTimer > 0)
        {
            wrongWayTimer = 0;
            isWarningShown = false;
            // สั่งปิด Warning ถ้ากลับตัวทัน
            // if (TrafficLawManager.Instance.warningPopup != null) TrafficLawManager.Instance.warningPopup.gameObject.SetActive(false);
        }
    }
}