using UnityEngine;
using TMPro; 
using System.Collections;
using Gley.TrafficSystem;

public class SpeedMonitor : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI speedTextUI;   
    public TextMeshProUGUI warningPopup;  

    [Header("Audio")]
    public AudioSource audioSource;       
    public AudioClip warningBeepSound;    

    [Header("Time Settings")]
    public float currentSpeedLimit = 40f; 

    [Header("Difficulty Settings (ความยาก)")]
    [Tooltip("ค่าอนุโลม: ขับเกินได้กี่ km/h ถึงจะไม่โดนจับ")]
    public float speedTolerance = 5f; // ค่าเริ่มต้น (Medium)
    
    [Tooltip("ระยะเวลาเตือนนิ่งๆ (ลดเหลือ 0.5 วิ ตามที่ขอ)")]
    public float staticWarningTime = 0.5f; 

    [Tooltip("ระยะเวลากระพริบเตือน (3.5 วิ)")]
    public float blinkingTime = 3.5f;    

    [Tooltip("ความถี่การกระพริบ")]
    public float blinkInterval = 0.5f;   

    [Tooltip("เวลาแช่แข็งเลขแดง")]
    public float freezeTime = 1.0f;       
    
    [Tooltip("เวลาพักใจ")]
    public float cooldownTime = 2.0f;     
    public int fineAmount = 10;           

    // ตัวแปรภายใน
    private float _timer;
    private Rigidbody _rb;
    private bool _isPenaltyFrozen = false; 
    private bool _isInCooldown = false;    

    // ตัวแปรสำหรับการกระพริบ
    private float _blinkTimer;
    private bool _isTextVisible = true;
    private bool _hasBeepedInThisCycle = false;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();

        // --- เพิ่มท่อนนี้เข้าไปครับ ---
        // เช็คชื่อฉากปัจจุบัน
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (sceneName.Contains("Easy") || sceneName.Contains("easy"))
        {
            // 🐣 โหมดง่าย: ใจดีมาก
            speedTolerance = 8f; // เกินได้ 8
            blinkingTime = 3.5f;  // ให้เวลาเบรกนานๆ
        }
        else if (sceneName.Contains("Hard") || sceneName.Contains("hard"))
        {
            // 🔥 โหมดโหด: เป๊ะเวอร์
            speedTolerance = 2f;  // เกินได้แค่ 2
            blinkingTime = 2.0f;  // จับไวมาก!
        }
        // ---------------------------
    }

    void OnTriggerEnter(Collider other)
    {
        SpeedLimitZone zone = other.GetComponent<SpeedLimitZone>();
        // ✅ ใส่ตรงนี้ครับ! ให้มันฟ้องเลยว่าชนอะไร
        if (zone != null)
        {
            Debug.Log($"✅ รถชนป้าย: {other.name} | เปลี่ยนความเร็วเป็น: {zone.maxSpeed}");
            currentSpeedLimit = zone.maxSpeed;
        }
        else
        {
            // ถ้าชนแล้วเงียบ แปลว่ารถชนอะไรไม่รู้ที่ไม่ใช่ป้ายความเร็ว
            // (ถ้าอยากรู้ว่าชนอะไร ให้ uncomment บรรทัดล่างดูครับ)
            // Debug.Log($"❌ รถชน: {other.name} (แต่มันไม่มีสคริปต์ SpeedLimitZone!)");
        }
    }

    void Update()
    {
        // 🛑 ป้องกัน error กรณีลืมลากตัวแปร
        if (_rb == null) return;

        // 🔍 Debug 1: เช็คว่าติด Cooldown จากการโดนจับรอบที่แล้วไหม?
        if (_isPenaltyFrozen || _isInCooldown) 
        {
            // ถ้าอยากเห็นว่าติดไหม ให้เอา // ข้างหน้าออกครับ
            // Debug.Log($"❄️ ระบบพักอยู่: Frozen={_isPenaltyFrozen}, Cooldown={_isInCooldown}");
            return;
        }

        // คำนวณความเร็ว
        #if UNITY_6000_0_OR_NEWER
             float speedMs = _rb.linearVelocity.magnitude;
        #else
             float speedMs = _rb.velocity.magnitude;
        #endif
        float currentSpeedKmH = speedMs * 3.6f;
        int speedInt = Mathf.RoundToInt(currentSpeedKmH);

        // 🔍 Debug 2: เช็คว่าระบบ Wrong Way มันบังอยู่ไหม?
        if (TrafficLawManager.Instance != null && TrafficLawManager.Instance.isWrongWayActive)
        {
            // ถ้า Console ขึ้นบรรทัดนี้ตอนขับเข้าวงเวียน แปลว่าสถานะ Wrong Way ค้าง!
            // Debug.Log("⚠️ ข้ามการจับความเร็ว เพราะติดสถานะ Wrong Way");
            
            if (speedTextUI != null) 
            {
                speedTextUI.gameObject.SetActive(true);
                speedTextUI.text = speedInt.ToString();
                speedTextUI.color = Color.white; 
            }
            // ปิด Popup Speed Limit (แต่เปิด Wrong Way ไว้)
            if (warningPopup != null && warningPopup.text.Contains("Speed Limit"))
            {
                warningPopup.gameObject.SetActive(false);
            }
            return; 
        }
        else
        {
            // ถ้าปกติ ให้โชว์ความเร็วไว้ก่อน
            if (speedTextUI != null) speedTextUI.gameObject.SetActive(true);
        }

        // เช็คว่าระบบเปิดอยู่ไหม
        bool isSystemOff = (TrafficLawManager.Instance != null && !TrafficLawManager.Instance.isMonitoring);
        if (isSystemOff) 
        {
            HandleNormalSpeed(speedInt);
            return; 
        }

        // --- ⚖️ Logic ตัดสิน ---
        
        // 🔍 Debug 3: ดูค่าจริงๆ กันไปเลยว่าทำไมไม่จับ
        if (currentSpeedKmH > (currentSpeedLimit + speedTolerance)) 
        {
            // ขับเกิน > เข้าข่ายโดนจับ
            HandleOverSpeed(speedInt);
        }
        else
        {
            // ขับไม่เกิน > รอด
            // ถ้าสงสัยว่าทำไมรอด ให้ดู Log นี้ครับ
            if (currentSpeedKmH > currentSpeedLimit)
            {
                 Debug.Log($"✅ รอดตัว (ยังอยู่ในเกณฑ์หยวนๆ): ขับ {speedInt} / ลิมิต {currentSpeedLimit} (Tolerance {speedTolerance})");
            }
            
            HandleNormalSpeed(speedInt);
        }
    }

    void HandleOverSpeed(int currentSpeedInt)
    {
        _timer += Time.deltaTime;
        float totalTime = staticWarningTime + blinkingTime;

        // 🟡 ระยะที่ 1: เตือนนิ่งๆ (0.5 วินาทีแรก)
        if (_timer < staticWarningTime)
        {
            if (speedTextUI != null)
            {
                speedTextUI.color = Color.yellow;
                speedTextUI.text = currentSpeedInt.ToString();
            }

            if (warningPopup != null)
            {
                warningPopup.gameObject.SetActive(true);
                warningPopup.text = $"<color=yellow>Speed Limit  {currentSpeedLimit}</color>";
            }
            
            // เตรียมตัวกระพริบ
            _blinkTimer = 0;
            _isTextVisible = true;
            _hasBeepedInThisCycle = false;
        }
        // 🟠 ระยะที่ 2: กระพริบ + เสียง
        else if (_timer < totalTime)
        {
            _blinkTimer += Time.deltaTime;

            if (_blinkTimer >= blinkInterval)
            {
                _blinkTimer = 0;
                _isTextVisible = !_isTextVisible; // สลับ เปิด/ปิด
                _hasBeepedInThisCycle = false;    // อนุญาตให้ดังรอบใหม่
            }

            // เล่นเสียงติ๊ด (ดังแค่ครั้งเดียวต่อรอบการกระพริบ)
            if (_isTextVisible && !_hasBeepedInThisCycle)
            {
                if (audioSource != null && warningBeepSound != null)
                {
                    // ใช้ PlayOneShot เพื่อไม่ให้เสียงตีกัน
                    audioSource.PlayOneShot(warningBeepSound, 1.25f);
                }
                _hasBeepedInThisCycle = true; 
            }

            // แสดงผล UI
            if (speedTextUI != null) 
            {
                speedTextUI.color = Color.yellow;
                speedTextUI.text = _isTextVisible ? currentSpeedInt.ToString() : ""; 
            }

            if (warningPopup != null)
            {
                warningPopup.gameObject.SetActive(true);
                string msg = $"<color=yellow>Speed Limit  {currentSpeedLimit}</color>";
                warningPopup.text = _isTextVisible ? msg : "";
            }
        }
        // 🔴 หมดเวลา!
        else
        {
            StartCoroutine(TriggerPenalty(currentSpeedInt));
        }
    }

    void HandleNormalSpeed(int currentSpeedInt)
    {
        // ✅ สั่ง Reset ทุกอย่างทันทีที่ความเร็วลดลง
        ResetWarningState();

        if (speedTextUI != null) 
        {
            speedTextUI.color = Color.white;
            speedTextUI.text = currentSpeedInt.ToString();
        }

        // ซ่อน Popup เฉพาะที่เป็น Speed Limit
        if (TrafficLawManager.Instance != null && TrafficLawManager.Instance.isMonitoring)
        {
             if (warningPopup != null && warningPopup.text.Contains("Speed Limit"))
             {
                 warningPopup.gameObject.SetActive(false);
             }
        }
    }

    // ฟังก์ชันล้างค่า (เพื่อตัดเสียงและไฟกระพริบ)
    void ResetWarningState()
    {
        _timer = 0;
        _blinkTimer = 0;
        _isTextVisible = true;
        _hasBeepedInThisCycle = false;
        
        // 🛑 สั่งหยุดเสียงทันที (เผื่อเสียงติ๊ดยาว)
        if (audioSource != null) audioSource.Stop();
    }

    IEnumerator TriggerPenalty(int caughtSpeed)
    {
        _isPenaltyFrozen = true; 
        
        // ✅ สั่ง Reset สถานะการเตือนทันที (เสียงติ๊ดจะหยุดตรงนี้ ไม่ดังต่อ)
        ResetWarningState();

        if (TrafficLawManager.Instance != null)
        {
            // 👇 [แทรกตรงนี้]
            TrafficLawManager.Instance.SpeedingCount++;
            
            TrafficLawManager.Instance.ReportViolation("SPEEDING", fineAmount);
        }

        // แดงค้าง
        if (speedTextUI != null)
        {
            speedTextUI.color = Color.red; 
            speedTextUI.text = caughtSpeed.ToString(); 
        }

        if (warningPopup != null)
        {
            warningPopup.gameObject.SetActive(true);
            warningPopup.text = $"<color=red>Speeding (-{fineAmount})</color>";
        }

        yield return new WaitForSeconds(freezeTime);
        
        _isPenaltyFrozen = false; 
        _isInCooldown = true;     

        if (warningPopup != null) warningPopup.gameObject.SetActive(false);
        if (speedTextUI != null) speedTextUI.color = Color.white; 

        // Cooldown Loop
        float cooldownTimer = 0;
        while (cooldownTimer < cooldownTime)
        {
            cooldownTimer += Time.deltaTime;
            #if UNITY_6000_0_OR_NEWER
                 float tempSpeed = _rb.linearVelocity.magnitude * 3.6f;
            #else
                 float tempSpeed = _rb.velocity.magnitude * 3.6f;
            #endif
            if (speedTextUI != null) speedTextUI.text = Mathf.RoundToInt(tempSpeed).ToString();
            yield return null; 
        }

        _isInCooldown = false; 
    }
}