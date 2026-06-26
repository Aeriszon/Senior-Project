using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // ✅ ต้องมีบรรทัดนี้

public class GameFlowManager : MonoBehaviour
{
    [Header("UI Settings")]
    public GameObject gameOverPanel;        
    public GameObject[] hudToHide;          

    [Header("Audio Settings")]
    public AudioSource engineAudioSource;   
    public AudioSource uiAudioSource;       
    public AudioClip gameOverClip;        

    // ✅ เพิ่มบรรทัดนี้ครับ:
    [Range(0f, 1f)] public float gameOverVolume = 0.5f;  

    [Header("State")]
    public bool isGameOver = false;

    public static GameFlowManager Instance;

    /*/ Awake(): ใช้ระบบ Singleton (Instance = this) เพื่อให้สคริปต์อื่นๆ ในเกม 
    (เช่น CheckpointRandomizer หรือ TrafficLawManager) สามารถตะโกนเรียก GameFlowManager.Instance ได้ทันทีโดยไม่ต้องเสียเวลาไปค้นหา /*/
    
    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    /*/ Start(): เป็นการเซ็ตค่าเริ่มต้นตอนเปิดด่านใหม่ให้เป็นปกติ (ปลดล็อกเวลา timeScale = 1f, ซ่อนหน้าจอ Game Over) 
    ทีเด็ดคือบรรทัด ignoreListenerPause = true; ที่สั่งให้เสียง UI ยังเล่นได้อยู่แม้ว่าเกมจะถูกกดหยุด (Pause) ไปแล้ว/*/
    
    void Start()
    {
        isGameOver = false;
        Time.timeScale = 1f; // เริ่มเวลาโลกภายในเกม

        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (uiAudioSource != null) 
        {
            uiAudioSource.ignoreListenerPause = true; 
        }
    }

    /*/ บรรทัดนี้คือ Failsafe ถ้าระบบเกมโอเวอร์ไปแล้ว แต่ดันมีบั๊กอะไรสักอย่างพยายามเล่นเสียงเครื่องยนต์ขึ้นมาอีก 
    ฟังก์ชัน Update ที่รันอยู่ทุกเฟรมจะจับกด Mute และปรับ Volume เป็น 0 ทันที /*/
    
    void Update()
    {
        // ระบบปิดเสียงแบบย้ำคิดย้ำทำ (เผื่อหลุด)
        if (isGameOver && engineAudioSource != null)
        {
            if (engineAudioSource.isPlaying) engineAudioSource.Stop();
            if (engineAudioSource.volume > 0) engineAudioSource.volume = 0f;
            if (engineAudioSource.enabled) engineAudioSource.enabled = false;
            if (!engineAudioSource.mute) engineAudioSource.mute = true;
        }
    }

    // ระบบจบเกมแบบพ่ายแพ้ (GameOver)
    // เมื่อโดนหักคะแนนจนหมด หรือเวลาหมด ฟังก์ชันนี้จะถูกเรียกใช้งาน:
    // สับสวิตช์: สั่ง isGameOver = true เพื่อบอกทุกระบบให้หยุดทำงาน
    // ซ่อนและโชว์ UI: ปิด HUD เกมเพลย์ทั้งหมด แล้วเด้งหน้าต่าง Game Over ขึ้นมา
    /*/ ส่ง Data เข้าฐานข้อมูล (Data Persistence): ตรงนี้เชื่อมกับ Architecture ของนายเลย 
    มันจะแพ็กข้อมูลสถิติการทำผิดกฎจราจรทั้งหมด ส่งไปให้ StatsManager เซฟลง Database พร้อมประทับตราว่าด่านนี้ "FAIL" /*/
    /*/ ระบบดับเครื่องยนต์ขั้นเด็ดขาด: ไม่ได้แค่ปิด AudioSource ที่ลากมาใส่เท่านั้น แต่เขียนเผื่อกันเหนียว (Failsafe) 
    โดยการสั่งค้นหารถด้วย Tag "Player" แล้วไล่ปิดเสียงทุกชิ้นในรถทิ้งหมด (แก้ปัญหาคลาสสิกของ Unity ที่เสียงเครื่องยนต์ค้างตอนเกมโอเวอร์) /*/
    // แช่แข็งเวลา: สั่ง Time.timeScale = 0f; เพื่อหยุดทุกอย่างในฉาก
    
    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (hudToHide != null)
        {
            foreach (GameObject hud in hudToHide)
            {
                if (hud != null) hud.SetActive(false);
            }
        }

        // ✅ [แทรกตรงนี้] บันทึกสถิติว่า "FAIL" ก่อนที่เกมจะหยุด
        if (StatsManager.Instance != null && TrafficLawManager.Instance != null)
        {
            StatsManager.Instance.SaveLog(
                SceneManager.GetActiveScene().name, // ชื่อด่าน
                "FAIL (Game Over)",                 // สถานะ
                TrafficLawManager.globalScore,      // คะแนน
                TrafficLawManager.Instance.RedLightCount, // ⚠️ เช็คชื่อตัวแปรนี้ใน TrafficLawManager นายด้วย
                TrafficLawManager.Instance.SpeedingCount,
                TrafficLawManager.Instance.HitCarCount,
                TrafficLawManager.Instance.HitBuildingCount,
                TrafficLawManager.Instance.HitObstacleCount,
                TrafficLawManager.Instance.SidewalkCount,
                TrafficLawManager.Instance.WrongWayCount // 👈 เพิ่มตัวนี้ต่อท้ายสุด!
            );
        }
        // ----------------------------------------------------

        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        // --- 🛑 ระบบปิดเสียงขั้นเด็ดขาด (แก้ใหม่) ---
        
        // 1. ปิดจากตัวแปรที่ลากใส่มา
        if (engineAudioSource != null)
        {
            engineAudioSource.mute = true;
            engineAudioSource.volume = 0f;
            engineAudioSource.Stop();
            engineAudioSource.enabled = false;
        }

        // 2. ✅ เพิ่ม: ค้นหารถทั้งฉากแล้วสั่งปิด (กันเหนียวสำหรับด่านอื่น)
        GameObject playerCar = GameObject.FindGameObjectWithTag("Player");
        if (playerCar != null)
        {
            AudioSource[] allAudio = playerCar.GetComponentsInChildren<AudioSource>();
            foreach (var audio in allAudio)
            {
                audio.Stop();
                audio.enabled = false;
            }
        }
        // ------------------------------------------

        if (uiAudioSource != null && gameOverClip != null)
        {
            uiAudioSource.ignoreListenerPause = true;
            uiAudioSource.PlayOneShot(gameOverClip, gameOverVolume);
        }

        Time.timeScale = 0f;
    }

    // ระบบจบเกมแบบผู้ชนะ (LevelCompleted)
    /*/ ฟังก์ชันนี้ถูกเรียกเมื่อผู้เล่นเข้า Checkpoint สุดท้ายได้สำเร็จ มันมีหน้าที่เข้าไป "ปิดระบบตรวจจับกฎจราจร" 
    (isMonitoring = false) เพื่อป้องกันบั๊กที่ผู้เล่นอาจจะขับรถไถลไปชนฟุตบาทหลังเข้าเส้นชัยแล้วโดนหักคะแนนย้อนหลัง /*/
    
    // ✅ เพิ่มฟังก์ชันนี้: สั่งให้เกมรับรู้ว่า "จบแล้ว (ชนะ)"
    public void LevelCompleted()
    {
        if (isGameOver) return;

        // ✅ 4. เพิ่มบรรทัดนี้: สั่งปิดตำรวจจราจรทันทีที่แตะเส้นชัย!
        if (TrafficLawManager.Instance != null)
        {
            TrafficLawManager.Instance.isMonitoring = false;
        }
    
        isGameOver = true; // สับสวิตช์นี้ปุ๊บ OffRoadSystem และ Traffic จะหยุดทำงานทันที!
        
        // หมายเหตุ: ส่วนหน้าต่าง Victory นายอาจจะสั่งเปิดจาก CheckpointManager อยู่แล้ว
        // แต่การสับตัวแปร isGameOver ตรงนี้สำคัญที่สุดครับ!
    }

    // ระบบเปลี่ยนฉากและเคลียร์ข้อมูล (RestartGame & QuitGame)
    // เลือกใช้ Coroutine (ระบบรอเวลา) แทนการเปลี่ยนฉากทันที:
    /*/ หน่วงเวลาเพื่อ User Experience: เมื่อผู้เล่นกดปุ่ม มันจะสั่งปิดเสียงเครื่องยนต์ก่อน แล้ว "รอ 0.5 วินาที" 
    (yield return new WaitForSecondsRealtime) เพื่อเปิดโอกาสให้เสียงกดปุ่ม (Click Sound) ได้เล่นจนจบก่อนที่หน้าจอจะตัดไป /*/
    // เคลียร์คะแนนให้ถูกต้อง: ถ้ากด Restart: ระบบจะคืนค่าคะแนนกลับไปเท่ากับ "ตอนที่เริ่มด่านนี้" (ไม่ใช่ 100 เสมอไป เพราะบางด่านอาจจะสานต่อคะแนนจากด่านก่อนหน้า)
    // ถ้ากด Quit: ระบบจะล้างไพ่ คืนค่า Global Score กลับเป็น 100 เต็ม แล้วเตะกลับหน้า Main Menu

    // --- ✅ แก้ส่วนนี้กลับมาเป็น Coroutine (เสียงคลิกจะได้ไม่หาย) ---

    public void RestartGame()
    {
        StartCoroutine(RestartRoutine());
    }

    IEnumerator RestartRoutine()
    {
        // 1. ใบ้เสียงเครื่องยนต์ก่อน
        if (engineAudioSource != null)
        {
            engineAudioSource.Stop();
            engineAudioSource.volume = 0f;
        }

        // 2. ⏳ รอ 0.5 วินาที ให้เสียงคลิกเล่นจบ
        yield return new WaitForSecondsRealtime(0.5f);

        // 👇 แทรกบรรทัดนี้: รีเซ็ตคะแนนกลับไปเท่าตอนเริ่มด่าน
        if (TrafficLawManager.Instance != null) TrafficLawManager.ResetScoreToLevelStart();

        // 3. ค่อยไปต่อ
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        StartCoroutine(QuitRoutine());
    }

    IEnumerator QuitRoutine()
    {
        // 1. ใบ้เสียงเครื่องยนต์
        if (engineAudioSource != null)
        {
            engineAudioSource.Stop();
            engineAudioSource.volume = 0f;
        }

        // 2. ⏳ รอ 0.5 วินาที
        yield return new WaitForSecondsRealtime(0.5f);

        // 👇 แทรกบรรทัดนี้: ล้างคะแนนกลับเป็น 100
        TrafficLawManager.globalScore = 100;

        // 3. ออกเกม
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu"); 
    }
}
