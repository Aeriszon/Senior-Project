using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class TrafficLawManager : MonoBehaviour
{
    public static TrafficLawManager Instance;

    // 1. เพิ่มตัวแปรเก็บ Coroutine ไว้ข้างบน
    private Coroutine _currentPopupRoutine;

    // ✅ 1. เพิ่มตัวแปรนี้ (เปิดไว้เป็น Default)
    public bool isMonitoring = true;

    [Header("Control References")]
    // ⚠️ ลากสคริปต์บังคับรถ (เช่น PrometheusController) มาใส่ช่องนี้
    public MonoBehaviour playerCarController; 
    // ⚠️ ลากสคริปต์ PauseManager มาใส่ช่องนี้
    public MonoBehaviour pauseManager;
    // ✅ เพิ่มตัวแปรเก็บ Rigidbody
    private Rigidbody playerRb;
    private RigidbodyConstraints originalConstraints;

    [Header("🔧 System Control")]
    public bool isTrafficSystemActive = true; 

    [Header("Global Data")]
    public static int globalScore = 100;        
    public int scoreAtLevelStart;

    // ✅ [เพิ่มชุดนี้เข้าไป] ถังเก็บสถิติสำหรับส่งให้ StatsManager
    [Header("📊 Statistics Counters")]
    public int RedLightCount = 0;      // ฝ่าไฟแดง
    public int SpeedingCount = 0;      // ขับเร็ว
    public int HitCarCount = 0;        // ชนรถ
    public int HitBuildingCount = 0;   // ชนตึก
    public int HitObstacleCount = 0;   // ชนของ
    public int SidewalkCount = 0;      // ปีนฟุตบาท
    public int WrongWayCount = 0;      // ขับย้อนศร

    [Header("UI References")]
    public TextMeshProUGUI warningPopup; 
    public TextMeshProUGUI scoreText;    

    [Header("Audio")]
    public AudioSource audioSource;      
    public AudioClip penaltySound;
    public AudioClip countdownSound; // ✅ เพิ่มช่องใส่เสียงนับถอยหลังตรงนี้!

    [Header("Visual Settings")]
    public Color safeColor = Color.white;
    public Color warningColor = Color.yellow;
    public Color dangerColor = Color.red;

    [Header("UI References")]
    public GameObject hudPanel;

    // ... (ส่วนประกาศตัวแปรข้างบน) ...
    [Header("UI Timing")]
    public float wrongWayExitDelay = 2.0f; // ⏳ หน่วงเวลา
    private Coroutine _restoreSpeedUiRoutine; 

    // ✅ ตัวแปรสถานะ (ให้คนอื่นมาแอบดู)
    [HideInInspector]
    public bool isWrongWayActive = false;

    // ✅ เพิ่ม 2 บรรทัดนี้ (เอาไว้ลาก DistanceText กับ ArrowPointer มาใส่)
    public GameObject extraDistanceUI; 
    public GameObject extraArrowUI;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 1. สั่งปิดทุกอย่างเงียบกริบก่อน
        if (hudPanel != null) hudPanel.SetActive(false);
        if (extraDistanceUI != null) extraDistanceUI.SetActive(false); // 👈 ปิด
        if (extraArrowUI != null) extraArrowUI.SetActive(false);       // 👈 ปิด
        
        // 🚩 แก้จุดที่ 2: เช็คชื่อด่านเพื่อรีเซ็ตคะแนน (แก้บั๊ก Restart แล้วคะแนนไม่เต็ม 100)
        string currentScene = SceneManager.GetActiveScene().name;

        // ⚠️ เปลี่ยน "Level_Easy" เป็นชื่อฉากจริงๆ ของนายนะ (เช่น "Easy", "Level1")
        if (currentScene == "Level_1_Easy" || currentScene == "Easy") 
        {
            globalScore = 100; // ถ้าเป็นด่านแรก บังคับให้เริ่มที่ 100 เสมอ
        }

        // จดคะแนนเริ่มต้นของด่านนี้ไว้ (ด่านแรกจะจด 100, ด่านสองจะจดคะแนนที่เหลือจากด่านแรก)
        scoreAtLevelStart = globalScore;
        UpdateScoreUI(); 

        // ✅ 1. เพิ่มชุดนี้ไว้บนสุด! แก้คำสาปเสียงหายตอน Restart
        AudioListener.pause = false; // ปลดล็อกเสียงที่ค้างจากหน้า Pause
        AudioListener.volume = 1.0f; // มั่นใจว่าเปิด Volume สุด
        Time.timeScale = 1.0f;       // มั่นใจว่าเวลาเดินปกติ
        
        // หา Rigidbody ของรถเตรียมไว้
        if (playerCarController != null) 
        {
            playerRb = playerCarController.GetComponent<Rigidbody>();
        }

        // 2. เรียก Cinematic Start
        StartCoroutine(CinematicStartSequence());
    }

    // --- โซนแจ้งเตือนเริ่มต้นเกม ---
    IEnumerator StartupWarningRoutine()
    {
        if (warningPopup != null)
        {
            warningPopup.gameObject.SetActive(true);
            
            // เริ่มนับจาก 5 (ช่วง 5 กับ 4 จะเงียบ)
            for (int i = 5; i > 0; i--)
            {
                // ✅ สั่งเล่นเสียงเฉพาะเมื่อถึงวินาทีที่ 3
                if (i == 3)
                {
                    if (audioSource != null && countdownSound != null)
                        audioSource.PlayOneShot(countdownSound);
                }

                warningPopup.text = $"<color=yellow>GET TO THE ROAD!  [{i}]</color>";
                yield return new WaitForSeconds(1.0f);
            }

            warningPopup.text = "<color=green>TRAFFIC SYSTEM ONLINE</color>";

            // ✅ ย้ายมาใส่ตรงนี้ครับ! ระบบ Online พร้อมข้อความเป๊ะๆ
            isMonitoring = true;

            yield return new WaitForSeconds(1.5f);
            warningPopup.gameObject.SetActive(false);
        }
        else
        {
            // (กันเหนียว) ถ้าไม่มี Popup ก็ให้เปิดระบบหลังนับเสร็จอยู่ดี
            isMonitoring = true; 
        }
    }
    // -----------------------------

    IEnumerator CinematicStartSequence()
    {
        isMonitoring = false;
        
        // 🔒 1. ล็อกปุ่ม Pause (เหมือนเดิม)
        if (pauseManager != null) pauseManager.enabled = false;

        // 🚗 2. ล็อกรถ (แบบใหม่: ไม่ปิดสคริปต์ แต่ล็อก Physics)
        if (playerRb != null)
        {
            // จำค่าเดิมไว้ก่อน (เผื่อรถมันมีการล็อกแกน Z หรืออะไรไว้)
            originalConstraints = playerRb.constraints;

            // สั่งล็อกตำแหน่ง X และ Z (เดินหน้า/เลี้ยว ไม่ได้) และล็อกการหมุน
            // แต่ปล่อย Y ไว้ (เผื่อรถเกิดกลางอากาศจะได้ตกลงพื้นได้ ไม่ลอยค้าง)
            playerRb.constraints = RigidbodyConstraints.FreezePositionX | 
                                   RigidbodyConstraints.FreezePositionZ | 
                                   RigidbodyConstraints.FreezeRotation;
        }

        // ⏳ รอ 2.5 วินาที (Cinematic)
        yield return new WaitForSeconds(2.5f); 

        // 🔓 3. ปลดล็อกทุกอย่าง
        if (pauseManager != null) pauseManager.enabled = true;

        if (playerRb != null)
        {
            // คืนค่า Physics เดิม -> รถพุ่งออกไปได้ทันที!
            playerRb.constraints = originalConstraints;
        }

        // เปิด HUD (Boom!)
        if (hudPanel != null) hudPanel.SetActive(true);
        if (extraDistanceUI != null) extraDistanceUI.SetActive(true);
        if (extraArrowUI != null) extraArrowUI.SetActive(true);
        
        // เริ่มนับถอยหลัง
        StartCoroutine(StartupWarningRoutine());
    }

    public void ReportViolation(string violationName, int fineAmount)
    {
        // ✅ 2. เพิ่มเช็คตรงนี้: ถ้าสั่งปิดระบบแล้ว หรือจบเกมแล้ว ห้ามหักคะแนนเด็ดขาด!
        if (!isMonitoring) return;
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.isGameOver) return;

        if (!isTrafficSystemActive) return;
        if (globalScore <= 0) return; 

        // 1. หักคะแนน
        globalScore -= fineAmount;
        if (globalScore < 0) globalScore = 0; 

        // 2. เสียงปี๊บ
        if (audioSource != null && penaltySound != null)
            audioSource.PlayOneShot(penaltySound);

        // 3. Popup แดง (แก้ตรงนี้!)
        if (warningPopup != null)
        {
            // สั่งหยุดตัวเก่อนหน้า (ถ้ามันยังรันอยู่)
            if (_currentPopupRoutine != null) StopCoroutine(_currentPopupRoutine); 
            
            // หยุดตัวนับถอยหลังเริ่มต้นเกมด้วย (กันพลาด)
            StopCoroutine("StartupWarningRoutine"); 

            // รันตัวใหม่ แล้วจำใส่ตัวแปรไว้
            _currentPopupRoutine = StartCoroutine(ShowPopupRoutine(violationName, fineAmount));
        }

        // 4. Score แดง
        if (scoreText != null)
        {
            StopCoroutine("FlashScoreRed");
            StartCoroutine(FlashScoreRed());
        }

        // 5. Game Over
        if (globalScore <= 0) TriggerGameOver();
    }

    public void UpdateScoreUI()
    {
        if (scoreText != null) 
        {
            Color currentColor = safeColor;
            if (globalScore <= 25) currentColor = dangerColor;
            else if (globalScore <= 50) currentColor = warningColor;

            string colorHex = ColorUtility.ToHtmlStringRGB(currentColor);
            scoreText.text = $"<color=#{colorHex}>{globalScore}</color>/100";
        }
    }

    IEnumerator FlashScoreRed()
    {
        scoreText.text = $"<color=red>{globalScore}</color>/100";
        yield return new WaitForSeconds(0.5f);
        UpdateScoreUI(); 
    }

    IEnumerator ShowPopupRoutine(string text, int amount)
    {
        warningPopup.text = $"<color=red>{text} (-{amount})</color>"; 
        warningPopup.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);
        warningPopup.gameObject.SetActive(false);
    }

    void TriggerGameOver()
    {
        Debug.Log("💀 GAME OVER");
        // หา Manager ของนายแล้วเรียก Game Over
        var gm = FindFirstObjectByType<GameFlowManager>(); // <-- แก้ชื่อ Class ตรงนี้ให้ตรงกับของนายนะ
        if (gm != null) gm.GameOver(); 
    }

    // --- Helper Functions สำหรับปุ่มกด ---
    public static void ResetScoreToLevelStart() 
    { 
        if (Instance != null) 
        {
            globalScore = Instance.scoreAtLevelStart; // ดึงค่าที่จดไว้ตอน Start กลับมา
            Instance.isMonitoring = true; // เปิดระบบให้พร้อมทำงานใหม่
            Instance.UpdateScoreUI();

            // ✅ [เพิ่มชุดนี้เข้าไป] ล้างสถิติให้เป็น 0
            Instance.RedLightCount = 0;
            Instance.SpeedingCount = 0;
            Instance.HitCarCount = 0;
            Instance.HitBuildingCount = 0;
            Instance.HitObstacleCount = 0;
            Instance.SidewalkCount = 0;
            Instance.WrongWayCount = 0;
        }
    }

    public static void SaveScoreForNextLevel() { /* ไม่ต้องใช้แล้วครับ เพราะ globalScore มันส่งไปเอง */ }

    // ⚠️ แก้บรรทัดนี้: เพิ่ม string colorName = "yellow" ต่อท้าย
    public void ShowWarning(string text, float duration, string colorName = "yellow")
    {
        if (!isMonitoring) return;
        
        if (warningPopup != null)
        {
            if (_currentPopupRoutine != null) StopCoroutine(_currentPopupRoutine);
            
            // ⚠️ แก้บรรทัดนี้: เอาตัวแปร colorName มาใส่แทนคำว่า yellow
            warningPopup.text = $"<color={colorName}>{text}</color>";
            warningPopup.gameObject.SetActive(true);

            _currentPopupRoutine = StartCoroutine(AutoClosePopup(duration));
        }
    }

    IEnumerator AutoClosePopup(float duration)
    {
        yield return new WaitForSeconds(duration);
        warningPopup.gameObject.SetActive(false);
    }

    // เพิ่มฟังก์ชันนี้ลงไปครับ เพื่อให้ LaneDirectionZone สั่งปิดป้ายได้
    public void HideWarning()
    {
        if (warningPopup != null)
        {
            warningPopup.gameObject.SetActive(false);
        }
    }

    // ✅ ฟังก์ชันรับคำสั่ง (เวอร์ชันหน่วงเวลา)
    public void SetWrongWayState(bool active)
    {
        if (active)
        {
            // 🚨 1. เข้าโหมด Wrong Way (หรือเข้ากล่องใหม่หลังข้ามแยก)
            
            // ยกเลิกการนับถอยหลังทันที! (ถือว่าย้อนศรต่อเนื่อง)
            if (_restoreSpeedUiRoutine != null) StopCoroutine(_restoreSpeedUiRoutine);
            
            isWrongWayActive = true; 
            
            // (Score ไม่ต้องปิด ตามที่ตกลงกัน)
            // (SpeedMonitor มันจะปิดตัวเองอัตโนมัติเมื่อเห็น isWrongWayActive = true)
        }
        else
        {
            // ✅ 2. ออกจากกล่อง (เข้าทางแยก/ทางม้าลาย)
            
            // "อย่าเพิ่งรีบปิดสถานะ!" ให้เริ่มนับถอยหลังก่อน
            if (_restoreSpeedUiRoutine != null) StopCoroutine(_restoreSpeedUiRoutine);
            _restoreSpeedUiRoutine = StartCoroutine(RestoreSpeedUILater());
        }
    }

    IEnumerator RestoreSpeedUILater()
    {
        // รอ 2 วินาที... (ช่วงนี้ป้าย Wrong Way จะยังค้างอยู่)
        yield return new WaitForSeconds(wrongWayExitDelay);

        // ถ้าครบ 2 วิ แล้วยังไม่มีใครสั่งย้อนศรเข้ามาใหม่
        // แปลว่าปลอดภัยจริงแล้ว -> ปลดสถานะได้!
        isWrongWayActive = false;
        
        // (SpeedMonitor จะเห็นว่า False แล้วเปิดเลขความเร็วกลับมาเอง)
    }
}