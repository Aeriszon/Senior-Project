using UnityEngine;
using UnityEngine.SceneManagement;

public class LaneDirectionZone : MonoBehaviour
{
    [Header("⚙️ Settings")]
    public bool isLinkedZone = false; 
    public float allowedAngle = 120f; 

    [Header("🔊 Audio Settings (เพิ่มใหม่)")]
    public AudioClip warningSound;       // 🎵 ลากไฟล์เสียง ติ๊ดๆ มาใส่ตรงนี้
    // ✅ ของใหม่ (อัดได้ถึง 5 เท่า!): 
    [Range(0, 5)] public float warningVolume = 2.0f; // ลองปรับเป็น 2 หรือ 3 ดูครับ

    [Header("Difficulty & Timing")]
    public float staticWarningTime = 0.5f; 
    public float blinkingDuration = 2.0f;  
    public float blinkInterval = 0.2f;     
    public float cooldownTime = 2.0f;      
    
    [Header("Penalty")]
    public int fineAmount = 10;

    // --- Static Variables ---
    private static float sharedTimer = 0f;
    private static float globalCooldownUntil = 0f;
    private static float lastActiveTime = 0f;

    // ✅ เพิ่ม 3 ตัวนี้เข้าไปครับ (เพื่อให้จังหวะหัวใจตรงกันทุกกล่อง)
    private static float sharedBlinkTimer = 0f;

    // ✅ เพิ่ม 2 ตัวนี้ครับ (ตัวกันบวกเวลาซ้ำ)
    private static int lastFrameTimerUpdated = -1;
    private static int lastFrameBlinkUpdated = -1;

    private static bool sharedIsTextVisible = true;
    private static bool sharedHasBeeped = false;

    // --- Local Variables ---
    private float localTimer = 0f;
    private float localCooldownUntil = 0f;

    // --- Internal State ---
    private bool isPlayerInZone = false;
    private Transform playerTransform;
    
    private float blinkTimer = 0f;
    private bool isTextVisible = true;
    private bool hasBeeped = false;
    private bool hasNotifiedManager = false; 

    void Start()
    {
        SetupDifficulty();
    }

    void SetupDifficulty()
    {
        string sceneName = SceneManager.GetActiveScene().name.ToLower();
        
        // ✅ ปรับจังหวะให้เหมือน Speed Limit เป๊ะๆ (เน้นความนิ่ง ไม่รลกวน)
        if (sceneName.Contains("easy"))
        {
            staticWarningTime = 0.5f; // เตือนนิ่ง 0.5 วิ
            blinkingDuration = 3.5f;  // กระพริบ/เสียง นานๆ (หนีทันสบาย)
            blinkInterval = 0.5f;     // จังหวะ: ติ๊ด... ติ๊ด... (0.5 วิ)
        }
        else if (sceneName.Contains("hard"))
        {
            // 🔥 Hard Mode: 
            // 1. เตือนนิ่งก่อน (ให้รู้ตัว)
            staticWarningTime = 0.5f; 
            
            // 2. กระพริบ/เสียง แค่แป๊บเดียว (1.5 วิ / 0.5 = ดังประมาณ 3 ครั้ง แล้วตูม!)
            blinkingDuration = 1.5f;  
            
            // 3. จังหวะ "คงที่" เท่าโหมด Easy (ไม่รัวยิกแล้ว)
            blinkInterval = 0.5f;     
        }
        else 
        {
            // 😐 Normal Mode
            staticWarningTime = 0.5f;
            blinkingDuration = 2.5f;
            blinkInterval = 0.5f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = true;
            playerTransform = other.transform;

            if (isLinkedZone)
            {
                if (Time.time - lastActiveTime > 0.5f)
                {
                    sharedTimer = 0f;
                    blinkTimer = 0f;
                    isTextVisible = true;
                    hasBeeped = false; // รีเซ็ตสถานะเสียง

                    // ✅ เพิ่ม 3 บรรทัดนี้ครับ (รีเซ็ตจังหวะด้วย)
                    sharedBlinkTimer = 0f;
                    sharedIsTextVisible = true;
                    sharedHasBeeped = false;
                }
            }
            else
            {
                localTimer = 0f;
                blinkTimer = 0f;
                isTextVisible = true;
                hasBeeped = false;
            }
            
            hasNotifiedManager = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            
            if (TrafficLawManager.Instance != null)
            {
                TrafficLawManager.Instance.HideWarning();
                TrafficLawManager.Instance.SetWrongWayState(false);
            }
            hasNotifiedManager = false;
        }
    }

    private void Update()
    {
        if (isPlayerInZone && playerTransform != null && TrafficLawManager.Instance != null && TrafficLawManager.Instance.isMonitoring)
        {
            CheckLogic();
        }
    }

    void CheckLogic()
    {
        float currentCooldown = isLinkedZone ? globalCooldownUntil : localCooldownUntil;
        if (Time.time < currentCooldown) 
        {
            // ช่วงพักใจ: สั่งคืนค่า Speed Limit ให้คนเล่นอุ่นใจ
            if (hasNotifiedManager)
            {
                 TrafficLawManager.Instance.SetWrongWayState(false);
                 TrafficLawManager.Instance.HideWarning();
                 hasNotifiedManager = false;
            }
            return;
        }

        float angle = Vector3.Angle(playerTransform.forward, transform.forward);

        if (angle > allowedAngle) // ❌ ย้อนศร
        {
            if (isLinkedZone) lastActiveTime = Time.time;
            
            if (!hasNotifiedManager)
            {
                TrafficLawManager.Instance.SetWrongWayState(true); // ปิด Speed Limit
                hasNotifiedManager = true;
            }

            // ✅ ของใหม่ (กันเบิ้ล):
            if (isLinkedZone) 
            {
                // เช็คว่าเฟรมนี้ มีใครบวกเวลาไปหรือยัง?
                if (lastFrameTimerUpdated != Time.frameCount)
                {
                    sharedTimer += Time.deltaTime;
                    lastFrameTimerUpdated = Time.frameCount; // จำไว้ว่าเฟรมนี้บวกแล้ว
                }
            }
            else localTimer += Time.deltaTime;
            
            float currentTimer = isLinkedZone ? sharedTimer : localTimer;
            float totalTime = staticWarningTime + blinkingDuration;

            // --- 🟡 ระยะที่ 1: เตือนนิ่งๆ ---
            if (currentTimer < staticWarningTime)
            {
                TrafficLawManager.Instance.ShowWarning("WRONG WAY!", 0.1f, "orange");
                blinkTimer = 0;
                isTextVisible = true;
            }
            // --- 🟠 ระยะที่ 2: กระพริบ + เสียง ---
            else if (currentTimer < totalTime)
            {
                HandleBlinking();
            }
            // --- 🔴 ระยะที่ 3: โดน! ---
            else
            {
                ApplyPenalty();
            }
        }
        else // ✅ ขับถูกทาง
        {
            if (isLinkedZone) sharedTimer = 0f;
            else localTimer = 0f;
            
            if (hasNotifiedManager)
            {
                TrafficLawManager.Instance.HideWarning();
                TrafficLawManager.Instance.SetWrongWayState(false); // เปิด Speed Limit คืน
                hasNotifiedManager = false;
            }
        }
    }

    void HandleBlinking()
    {
        // 1. เลือกใช้ตัวแปรให้ถูกชุด (Ref เพื่อชี้ไปที่ตัวแปรจริง)
        ref float timerRef = ref blinkTimer;
        ref bool visibleRef = ref isTextVisible;
        ref bool beepedRef = ref hasBeeped;

        // ถ้าเป็นวงเวียน/ทางโค้ง -> ให้ไปใช้ตัวแปร Static (ส่วนกลาง) แทน!
        if (isLinkedZone)
        {
            timerRef = ref sharedBlinkTimer;
            visibleRef = ref sharedIsTextVisible;
            beepedRef = ref sharedHasBeeped;
        }

        // 2. ลอจิกการกระพริบ (เหมือนเดิม แต่ทำงานบนตัวแปรที่เลือกมาแล้ว)
        // ✅ ของใหม่ (กันเบิ้ล):
        if (isLinkedZone)
        {
            // ถ้าเป็น Linked เช็คเฟรมก่อนบวก
            if (lastFrameBlinkUpdated != Time.frameCount)
            {
                timerRef += Time.deltaTime;
                lastFrameBlinkUpdated = Time.frameCount;
            }
        }
        else
        {
            // ถ้าเป็น Local บวกได้เลย (เพราะมันไม่มีทางซ้อนกับตัวเอง)
            timerRef += Time.deltaTime;
        }
        
        if (timerRef >= blinkInterval)
        {
            timerRef = 0;
            visibleRef = !visibleRef;
            beepedRef = false; // อนุญาตให้ดังรอบใหม่
        }

        // เล่นเสียงติ๊ด
        if (visibleRef && !beepedRef)
        {
            if (warningSound != null && TrafficLawManager.Instance.audioSource != null)
            {
                TrafficLawManager.Instance.audioSource.PlayOneShot(warningSound, warningVolume);
            }
            beepedRef = true;
        }

        // โชว์ข้อความ
        if (visibleRef) TrafficLawManager.Instance.ShowWarning("WRONG WAY!", 0.1f, "orange");
        else TrafficLawManager.Instance.HideWarning();
    }

    void ApplyPenalty()
    {
        Debug.Log("🚨 PENALTY APPLIED!");
        
        // 🛑 ตัดเสียงทันทีที่โดนจับ! (กันเสียงค้าง)
        if (TrafficLawManager.Instance.audioSource != null)
        {
             // หมายเหตุ: Stop() จะหยุดทุกเสียง ถ้าไม่อยากให้หยุด BGM อาจจะต้องแยก AudioSource
             // แต่วิธีนี้ชัวร์สุดสำหรับเสียง Loop ที่ค้าง
             // หรือถ้าใช้ PlayOneShot มันจะหยุดเองเมื่อจบไฟล์ แต่ถ้าไฟล์ยาวต้อง Stop
             // TrafficLawManager.Instance.audioSource.Stop(); 
        }

        // 👇 แทรกตรงนี้เลย (ก่อนบรรทัด ReportViolation)
        if (TrafficLawManager.Instance != null) 
        {
            TrafficLawManager.Instance.WrongWayCount++;
        }

        TrafficLawManager.Instance.ReportViolation("WRONG WAY", fineAmount);

        float cooldownEnd = Time.time + cooldownTime;
        
        if (isLinkedZone)
        {
            globalCooldownUntil = cooldownEnd;
            sharedTimer = 0f;
        }
        else
        {
            localCooldownUntil = cooldownEnd;
            localTimer = 0f;
        }

        // TrafficLawManager.Instance.HideWarning();
        
        // ✅ คืนค่า Speed Limit กลับมาทันที (เพื่อให้เห็นว่ากลับมาโหมดปกติแล้ว)
        TrafficLawManager.Instance.SetWrongWayState(false);
        hasNotifiedManager = false;
    }

    private void OnDrawGizmos()
    {
        // ... (ส่วนวาด Gizmo เดิม ไม่ต้องแก้) ...
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        if (isLinkedZone) Gizmos.color = new Color(1, 0.5f, 0, 0.2f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        Gizmos.color = Color.blue;
        Vector3 end = transform.position + transform.forward * 4.0f;
        Gizmos.DrawLine(transform.position, end);
        Gizmos.DrawSphere(end, 0.5f);
    }
}