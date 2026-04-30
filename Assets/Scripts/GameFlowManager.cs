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

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        isGameOver = false;
        Time.timeScale = 1f;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (uiAudioSource != null) 
        {
            uiAudioSource.ignoreListenerPause = true; 
        }
    }

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