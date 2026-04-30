using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // ต้องมีบรรทัดนี้เพื่อใช้ Coroutine

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pausePanel;     // หน้าเมนู Pause
    public GameObject warningPanel;   // ✅ เพิ่มตัวนี้: ป้ายเตือน Out of Area (จะได้สั่งปิดมันตอน Pause)
    
    // --- HUD References ---
    public GameObject gameplayUI;     // HUD หลัก
    public GameObject arrowPointer;   // ลูกศร
    public GameObject distanceText;   // เลขระยะทาง

    [Header("Audio Settings")]
    public AudioSource tickAudio;     // เสียงนาฬิกา
    
    private bool isPaused = false;

    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false; 
        
        // สั่งให้เสียง UI ดังทะลุ Pause (เผื่อไว้)
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.uiAudioSource != null)
        {
            GameFlowManager.Instance.uiAudioSource.ignoreListenerPause = true;
        }

        if (pausePanel != null) pausePanel.SetActive(false);
    }

    void Update()
    {
        // ✅ ป้องกันการกด Pause ถ้าเกมจบแล้ว (ชนะ, แพ้, หรือตกแมพตาย)
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.isGameOver) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        
        // 1. เปิดหน้า Pause
        if (pausePanel != null) pausePanel.SetActive(true);

        // 2. ซ่อน HUD ต่างๆ
        if (gameplayUI != null) gameplayUI.SetActive(false);
        if (arrowPointer != null) arrowPointer.SetActive(false);
        if (distanceText != null) distanceText.SetActive(false);

        // ✅ 3. ซ่อนป้ายเตือน Out of Area (แก้ปัญหาตัวหนังสือซ้อนกัน)
        if (warningPanel != null) warningPanel.SetActive(false);

        // 4. หยุดเวลาและเสียง
        Time.timeScale = 0f;
        AudioListener.pause = true; 

        if (tickAudio != null) tickAudio.Pause(); 
    }

    public void ResumeGame()
    {
        isPaused = false;
        
        // 1. ปิดหน้า Pause
        if (pausePanel != null) pausePanel.SetActive(false);

        // 2. เรียก HUD กลับมา
        if (gameplayUI != null) gameplayUI.SetActive(true);
        if (arrowPointer != null) arrowPointer.SetActive(true);
        if (distanceText != null) distanceText.SetActive(true);

        // (ไม่ต้องสั่งเปิด warningPanel กลับมา เดี๋ยวระบบ OffRoad มันเช็คแล้วเด้งเองถ้ายังตกแมพอยู่)

        // 3. เดินเวลาและเสียงต่อ
        Time.timeScale = 1f;
        AudioListener.pause = false; 

        if (tickAudio != null) tickAudio.UnPause(); 
    }

    public void RestartGame()
    {
        StartCoroutine(RestartRoutine());
    }

    IEnumerator RestartRoutine()
    {
        // ✅ [แทรกตรงนี้บนสุดเลย] บันทึกว่า "กดเริ่มใหม่เอง" (Restarted)
        if (StatsManager.Instance != null && TrafficLawManager.Instance != null)
        {
            StatsManager.Instance.SaveLog(
                SceneManager.GetActiveScene().name,
                "RESTARTED (User)",
                TrafficLawManager.globalScore,
                TrafficLawManager.Instance.RedLightCount,
                TrafficLawManager.Instance.SpeedingCount,
                TrafficLawManager.Instance.HitCarCount,
                TrafficLawManager.Instance.HitBuildingCount,
                TrafficLawManager.Instance.HitObstacleCount,
                TrafficLawManager.Instance.SidewalkCount,
                TrafficLawManager.Instance.WrongWayCount // 👈 เพิ่มตัวนี้ต่อท้ายสุด!
            );
        }
        // ----------------------------------------------------

        // 1. ฆ่าเสียงเครื่องยนต์ทิ้งก่อน
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.engineAudioSource != null)
        {
            GameFlowManager.Instance.engineAudioSource.volume = 0f;
            GameFlowManager.Instance.engineAudioSource.Stop();
            GameFlowManager.Instance.engineAudioSource.enabled = false;
        }

        // 2. ⏳ รอ 0.5 วินาที ให้เสียงคลิกเล่นจนจบ
        yield return new WaitForSecondsRealtime(0.5f);

        // 👇 แทรกบรรทัดนี้: รีเซ็ตคะแนนกลับไปเท่าตอนเริ่มด่าน
        if (TrafficLawManager.Instance != null) TrafficLawManager.ResetScoreToLevelStart();

        // 3. ค่อยโหลดฉาก
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        StartCoroutine(QuitRoutine());
    }

    IEnumerator QuitRoutine()
    {
        // ✅ [แทรกตรงนี้บนสุดเลย] บันทึกว่า "หนีกลับเมนู" (Quit)
        if (StatsManager.Instance != null && TrafficLawManager.Instance != null)
        {
            StatsManager.Instance.SaveLog(
                SceneManager.GetActiveScene().name,
                "QUIT (To Menu)",
                TrafficLawManager.globalScore,
                TrafficLawManager.Instance.RedLightCount,
                TrafficLawManager.Instance.SpeedingCount,
                TrafficLawManager.Instance.HitCarCount,
                TrafficLawManager.Instance.HitBuildingCount,
                TrafficLawManager.Instance.HitObstacleCount,
                TrafficLawManager.Instance.SidewalkCount,
                TrafficLawManager.Instance.WrongWayCount // 👈 เพิ่มตัวนี้ต่อท้ายสุด!
            );
        }
        // ----------------------------------------------------

        // 1. ฆ่าเสียงเครื่องยนต์
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.engineAudioSource != null)
        {
            GameFlowManager.Instance.engineAudioSource.volume = 0f;
            GameFlowManager.Instance.engineAudioSource.Stop();
            GameFlowManager.Instance.engineAudioSource.enabled = false;
        }

        // 2. ⏳ รอ 0.5 วินาที ให้เสียงคลิกเล่นจนจบ
        yield return new WaitForSecondsRealtime(0.5f);

        // 👇 แทรกบรรทัดนี้: ล้างคะแนนกลับเป็น 100
        TrafficLawManager.globalScore = 100;

        // 3. ค่อยโหลดฉากเมนู
        Time.timeScale = 1f; 
        SceneManager.LoadScene("MainMenu"); 
    }
}