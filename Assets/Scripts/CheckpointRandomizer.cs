using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class CheckpointRandomizer : MonoBehaviour
{

    /*/ ตัวแปรทั้งหมด (Headers): แบ่งหมวดหมู่ไว้ชัดเจนมาก ทั้งเวลา (Time Settings),
    UI, เสียง (Audio), และฉากจบ (Win Screen Settings) 
    การจัดระเบียบแบบนี้คือสิ่งที่ทำให้เรา Vibe Code หรือใช้ AI มาปรับแก้ค่าต่างๆ ได้ง่ายมาก /*/
    
    [Header("Settings")]
    public Transform checkpointPool;
    public int targetCheckpoints = 2;
    public Transform playerTransform; 

    [Header("Final Level Config")]
    public bool isFinalLevel = false;

    [Header("Time Settings")]
    public float initialTime = 120f;
    public float bonusTime = 10f;
    private float currentTime;
    
    [Header("Visual (Last Checkpoint)")]
    public Material greenDiskMat;     
    public Material greenCurtainMat;  
    public Color greenLightColor = Color.green;

    [Header("Audio - SFX")]
    public AudioSource audioSource; 
    public AudioSource engineAudioSource;
    public AudioClip normalSound;

    // ✅ เพิ่มบรรทัดนี้: ตัวปรับเสียงเก็บของ
    [Range(0f, 1f)] public float pickupVolume = 1.0f;

    public AudioClip winSound;      
    
    [Header("Audio - Warning")]
    public AudioClip tickClip;     
    [Range(0f, 1f)] public float beepVolume = 0.3f; 

    // ✅ เพิ่มบรรทัดนี้ครับ: ตัวปรับเสียง Game Over
    [Range(0f, 1f)] public float gameOverVolume = 0.5f;
    
    public AudioClip gameOverClip; 
    private float nextTickTime = 0f;

    [Header("UI Reference")]
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI timerText; 
    
    [Header("UI to Hide")]
    public GameObject arrowPointerUI;
    public GameObject distanceTextUI;
    public GameObject gameplayUI;

    [Header("Visual Effects")]
    public Color bonusColor = Color.green; 
    public Color normalColor = Color.white; 
    public Color warningColor = Color.red;  
    public Color finishTextColor = Color.green;
    private bool isFlashing = false; 

    [Header("Win Screen Settings")]
    public GameObject winPanel;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI titleText;
    public GameObject nextLevelButton; 
    public GameObject mainMenuButton;

    [Header("Bonus Settings")]
    public int cleanDriveBonus = 25; // 🚩 ปรับเลขตรงนี้ได้เลย (เช่น 10, 20, 25)

    private List<GameObject> allCheckpoints = new List<GameObject>(); 
    private GameObject currentActiveCP;
    private int collectedCount = 0;
    private bool isFinished = false;

    /*/ ฟังก์ชัน Start(): พอเปิดฉากมา มันจะทำหน้าที่ "จัดเตรียมเวที" โดยการดึงเสา Checkpoint ทั้งหมดไปซ่อนไว้ก่อน 
    (SetActive = false) รีเซ็ตเวลาให้เต็ม และสั่งเรียก Checkpoint ต้นแรกออกมาให้ผู้เล่นเห็น /*/
    
    void Start()
    {
        currentTime = initialTime;
        UpdateTimerUI();

        // if (arrowPointerUI != null) arrowPointerUI.SetActive(true);
        // if (distanceTextUI != null) distanceTextUI.SetActive(true);
        // if (gameplayUI != null) gameplayUI.SetActive(true);

        if (checkpointPool != null)
        {
            allCheckpoints.Clear();
            foreach (Transform child in checkpointPool) 
            {
                allCheckpoints.Add(child.gameObject);
                child.gameObject.SetActive(false); 
            }
        }

        if (playerTransform == null) playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        SpawnNextIntelligentCheckpoint();
        UpdateProgressUI();
        
        if (winPanel != null) winPanel.SetActive(false);
    }

    /*/ ฟังก์ชัน Update(): นี่คือหัวใจที่เต้นอยู่ตลอดเวลาของเกม มันจะนับเวลาถอยหลัง (Time.deltaTime)
    ทีเด็ดคือ "ระบบแจ้งเตือน 15 วินาทีสุดท้าย" ถ้าเวลาเหลือน้อย มันจะสั่งให้ตัวเลขเป็นสีแดง 
    และเล่นเสียง "ติ๊กต็อก" (tickClip) เป็นจังหวะเพื่อกดดันผู้เล่น (เป็น UX ที่กระตุ้นความตื่นเต้นได้ดีมาก) /*/
    
    void Update()
    {
        // ✅ เพิ่มบรรทัดนี้บนสุดของ Update เลยครับ
        // ถ้าเกมเพิ่งเริ่มไม่ถึง 3 วิ ไม่ต้องทำอะไร (เวลานิ่งอยู่ที่ 120)
        if (Time.timeSinceLevelLoad < 3.0f) return;

        if (!isFinished && currentTime > 0)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 0)
            {
                currentTime = 0;
                HandleTimeUp();
            }

            // --- Warning Tick Sound Logic ---
            if (currentTime <= 15f && currentTime > 0)
            {
                if (!isFlashing && timerText != null) timerText.color = warningColor;

                if (Time.time >= nextTickTime)
                {
                    if (currentTime <= 15f && audioSource != null && tickClip != null)
                    {
                        audioSource.PlayOneShot(tickClip, beepVolume); 
                    }
                    float interval = (currentTime <= 5f) ? 0.5f : 1.0f;
                    nextTickTime = Time.time + interval;
                }
            }
            else
            {
                if (!isFlashing && timerText != null) timerText.color = normalColor;
            }

            UpdateTimerUI();
        }
    }

    /*/ ฟังก์ชัน HandleTimeUp(): ถ้าเวลาหมดปุ๊บ ฟังก์ชันนี้จะสับสวิตช์ปิดเกมทันที 
    ปิดเสียงเครื่องยนต์ ซ่อน UI ทั้งหมด แล้วส่งสัญญาณบอก GameFlowManager ว่า "ผู้เล่นตายแล้ว (Game Over)" /*/
    
    void HandleTimeUp()
    {
        if (audioSource != null) 
        {
            audioSource.Stop(); 
            // ✅ แก้เป็นแบบนี้ครับ (ใส่ gameOverVolume เข้าไป)
            if (gameOverClip != null) audioSource.PlayOneShot(gameOverClip, gameOverVolume);
        }

        if (arrowPointerUI != null) arrowPointerUI.SetActive(false);
        if (distanceTextUI != null) distanceTextUI.SetActive(false);
        if (gameplayUI != null) gameplayUI.SetActive(false);
        if (engineAudioSource != null) engineAudioSource.Stop();

        GameFlowManager gm = FindFirstObjectByType<GameFlowManager>();
        if (gm != null) gm.GameOver(); 
    } 

    /*/ ฟังก์ชัน OnCheckpointCaptured(): เมื่อผู้เล่นขับรถชน Checkpoint มันจะบวกยอดสะสม (collectedCount++) 
    แล้วเช็กว่า "ครบหรือยัง?" ถ้าครบแล้วจะส่งไปฉากจบ (VictorySequence) ถ้ายังไม่ครบ จะบวกเวลาเพิ่ม (AddBonusTime) เล่นเสียงเก็บของ แล้วเสกเสาต้นต่อไป/*/
    
    public void OnCheckpointCaptured()
    {
        if (isFinished) return;
        if (currentActiveCP != null) currentActiveCP.SetActive(false);
        
        collectedCount++; 
        
        if (collectedCount >= targetCheckpoints) {
            StartCoroutine(VictorySequence());
        } else {
            UpdateProgressUI();
            AddBonusTime(); 
            if (audioSource != null && normalSound != null) audioSource.PlayOneShot(normalSound, pickupVolume);
            SpawnNextIntelligentCheckpoint();
        }
    }

    /*/ ฟังก์ชัน SpawnNextIntelligentCheckpoint(): อันนี้คือ "ลอจิกสุดล้ำ" ที่ไม่ได้สุ่มแบบหลับหูหลับตา 
    มันจะคำนวณหาระยะทาง (Distance) และเช็กทิศทางหน้ารถ (InverseTransformPoint) เพื่อให้ชัวร์ว่า 
    Checkpoint ต้นต่อไปจะโผล่ในระยะที่พอดี และอยู่ข้างหน้าผู้เล่นเสมอ ไม่โผล่มาข้างหลังให้ต้องถอยรถกลับ /*/
    
    void SpawnNextIntelligentCheckpoint()
    {
        if (playerTransform == null) return;
        if (allCheckpoints == null || allCheckpoints.Count == 0) return;

        List<GameObject> candidates = new List<GameObject>();
        float minDistance = 50f;  
        float maxDistance = 500f; 

        foreach (GameObject cp in allCheckpoints)
        {
            if (cp == currentActiveCP) continue;
            float dist = Vector3.Distance(playerTransform.position, cp.transform.position);
            Vector3 localPos = playerTransform.InverseTransformPoint(cp.transform.position);
            if (localPos.z > 0 && dist >= minDistance && dist <= maxDistance) candidates.Add(cp);
        }

        if (candidates.Count > 0)
        {
            currentActiveCP = candidates[Random.Range(0, candidates.Count)];
        }
        else
        {
            GameObject bestFallback = null;
            float maxDistFound = -1f;
            foreach (GameObject cp in allCheckpoints)
            {
                if (cp == currentActiveCP) continue;
                Vector3 lp = playerTransform.InverseTransformPoint(cp.transform.position);
                if (lp.z > 0) 
                {
                    float d = Vector3.Distance(playerTransform.position, cp.transform.position);
                    if (d > maxDistFound) { maxDistFound = d; bestFallback = cp; }
                }
            }
            currentActiveCP = bestFallback;
        }

        if (currentActiveCP == null)
        {
            GameObject oldCP = currentActiveCP;
            int attempts = 0;
            do {
                currentActiveCP = allCheckpoints[Random.Range(0, allCheckpoints.Count)];
                attempts++;
            } while (currentActiveCP == oldCP && attempts < 10 && allCheckpoints.Count > 1); 
        }

        if (currentActiveCP != null)
        {
            currentActiveCP.SetActive(true);
            if (collectedCount == targetCheckpoints - 1) ApplyFinalCheckpointVisuals(currentActiveCP);
        }
    }

    /*/ ฟังก์ชัน ApplyFinalCheckpointVisuals(): พอถึง Checkpoint ต้นสุดท้าย 
    มันจะสั่งเปลี่ยนสีเสาเป็นสีเขียว (greenDiskMat) เพื่อบอกใบ้ให้ผู้เล่นรู้ว่า "นี่คือเส้นชัย!" /*/
    
    void ApplyFinalCheckpointVisuals(GameObject cp)
    {
        MeshRenderer[] renderers = cp.GetComponentsInChildren<MeshRenderer>();
        foreach (var r in renderers)
        {
            string objName = r.name.ToLower();
            if (objName.Contains("base")) 
            {
                 if (greenCurtainMat != null) r.material = greenCurtainMat; 
            }
            else if (objName.Contains("checkpoint"))
            {
                 if (greenDiskMat != null) r.material = greenDiskMat; 
            }
            else 
            {
                 if (greenDiskMat != null) r.material = greenDiskMat; 
            }
        }
        Light l = cp.GetComponentInChildren<Light>();
        if (l != null) l.color = greenLightColor;
    }

    public void AddBonusTime()
    {
        currentTime += bonusTime;
        
        if (currentTime > 15f)
        {
            if (audioSource != null) audioSource.Stop(); 
            if (audioSource != null) audioSource.clip = null; 
            if (timerText != null) timerText.color = normalColor;
            nextTickTime = Time.time + 2.0f; 
        }

        UpdateTimerUI();
        if (timerText != null) StartCoroutine(FlashTimerEffect());
    }
    
    IEnumerator FlashTimerEffect()
    {
        isFlashing = true;
        if (timerText != null) timerText.color = bonusColor;
        yield return new WaitForSeconds(0.5f);
        isFlashing = false;
    }

    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60F);
            int seconds = Mathf.FloorToInt(currentTime % 60F);
            timerText.text = string.Format("{0:0}:{1:00}", minutes, seconds);
        }
    }

    void UpdateProgressUI()
    {
        if (progressText != null)
        {
            progressText.text = $"{collectedCount}/{targetCheckpoints}";
            progressText.color = normalColor;
        }
    }
    
    public void LoadNextLevel()
    {
        Time.timeScale = 1f; 
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex < SceneManager.sceneCountInBuildSettings) SceneManager.LoadScene(nextIndex);
        else SceneManager.LoadScene("MainMenu");
    }
    
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;

        // 👇 แทรกบรรทัดนี้: ล้างคะแนนกลับเป็น 100
        TrafficLawManager.globalScore = 100;
        
        SceneManager.LoadScene("MainMenu");
    }

    public Transform GetCurrentTarget()
    {
        if (currentActiveCP != null) return currentActiveCP.transform;
        return null;
    }

    // ฉากจบและการสื่อสารข้ามระบบ (VictorySequence)
    // หยุดเวลาทันที และ สับสวิตช์ปิดระบบจับผิดของตำรวจจราจร (TrafficLawManager) เพื่อไม่ให้โดนหักคะแนนย้อนหลัง
    // ส่ง Data สถิติการขับขี่ทั้งหมด (ฝ่าไฟแดง, ชนรถ, ปีนฟุตบาท, ขับย้อนศร) ไปเซฟลงฐานข้อมูลผ่าน StatsManager.Instance.SaveLog()
    // ปิดระบบแจ้งเตือนออกนอกเลน (OffRoadSystem) และล็อกปุ่ม Pause เกม
    // คำนวณ "คะแนนโบนัสขับขี่ปลอดภัย" แล้วส่งไม้ต่อให้ฟังก์ชันแอนิเมชันสรุปคะแนน
    
    // --- แก้ไขจุดนี้ครับ (VictorySequence) ---
    IEnumerator VictorySequence()
    {
        isFinished = true; // หยุดการนับเวลาทันที

        // ✅ 1. เพิ่มบรรทัดนี้ด่วน! สั่งปิดตำรวจจราจรทันทีที่เก็บครบ
        if (TrafficLawManager.Instance != null) 
        {
            TrafficLawManager.Instance.isMonitoring = false;
        }

        // ✅ [แทรกตรงนี้] บันทึกสถิติว่า "PASS" 
        if (StatsManager.Instance != null && TrafficLawManager.Instance != null)
        {
            StatsManager.Instance.SaveLog(
                SceneManager.GetActiveScene().name,
                "PASS (Completed)",                 // สถานะ
                TrafficLawManager.globalScore,      // คะแนน (เดี๋ยวข้างล่างมันบวกโบนัสเพิ่มทีหลัง ไม่เป็นไร เอาคะแนนดิบก่อน)
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

        // ------------------ [ก๊อปชุดนี้ไปแปะต่อท้ายเลยครับ] ------------------
        
        // 1. ปิดระบบ Out of Area (กันเตือนแดงขึ้นตอนขับรถเล่นรอจบเกม)
        OffRoadSystem offRoad = FindFirstObjectByType<OffRoadSystem>();
        if (offRoad != null)
        {
            if (offRoad.warningPanel != null) offRoad.warningPanel.SetActive(false); // ซ่อนป้ายทิ้ง
            offRoad.enabled = false; // สับคัทเอาท์ดับสคริปต์ไปเลย
        }

        // 2. ปิดระบบ Pause (กันมือลั่นกด ESC ตอนจบ)
        PauseManager pauseMgr = FindFirstObjectByType<PauseManager>();
        if (pauseMgr != null)
        {
            pauseMgr.enabled = false; // สับคัทเอาท์ -> ปุ่ม ESC จะกดไม่ติดทันที
        }
        
        // -------------------------------------------------------------------

        // 🛑 [เพิ่มใหม่] สั่งหยุดเสียงนาฬิกาทันที ก่อนจะทำอย่างอื่น!
        if (audioSource != null) 
        {
            audioSource.Stop(); 
            audioSource.clip = null; // เคลียร์คลิปทิ้งกันเหนียว
        }
        // ------------------------------------------------

        // บันทึกคะแนน (ถ้ามีไฟล์ TrafficLawManager แล้วให้เอา // ออก)
        if (TrafficLawManager.Instance != null) TrafficLawManager.SaveScoreForNextLevel();

        if (progressText != null) 
        {
            progressText.text = "FINISH!";
            progressText.color = finishTextColor;
        }

        // เล่นเสียงชนะ (ตอนนี้จะไม่มีเสียงติ๊กต็อกมากวนแล้ว)
        if (audioSource != null && winSound != null) 
        {
            audioSource.PlayOneShot(winSound);
        }

        if (arrowPointerUI != null) arrowPointerUI.SetActive(false);
        if (distanceTextUI != null) distanceTextUI.SetActive(false);

        yield return new WaitForSeconds(3.0f);

        if (engineAudioSource != null) engineAudioSource.Stop();
        if (gameplayUI != null) gameplayUI.SetActive(false);
        if (winPanel != null) winPanel.SetActive(true);

        // ==================== ✂️ เริ่มแก้ตรงนี้ ✂️ ====================

        // 1. ดึงคะแนน
        int currentScore = 100;
        int startScore = 100;
        if (TrafficLawManager.Instance != null) {
            currentScore = TrafficLawManager.globalScore;
            startScore = TrafficLawManager.Instance.scoreAtLevelStart;
        }

        // 2. คำนวณโบนัส (เหมือนเดิม)
        int bonusAdded = 0;
        // เช็คว่าถ้าคะแนนยังไม่เต็ม และขับไม่โดนหักเลย ให้บวกเพิ่ม
        if (currentScore >= startScore && currentScore < 100) {
            int spaceLeft = 100 - currentScore;
            bonusAdded = Mathf.Min(cleanDriveBonus, spaceLeft);
        }

        // อัปเดตคะแนนจริงเข้าระบบ
        if (TrafficLawManager.Instance != null) TrafficLawManager.globalScore += bonusAdded;
        int finalTotalScore = currentScore + bonusAdded;

        // 3. จัดการ Title
        if (titleText != null) {
            titleText.text = isFinalLevel ? "ALL LEVELS CLEARED!" : "LEVEL COMPLETED!";
        }

        // 4. เรียกแอนิเมชั่น (ใช้ตัวเดียวกันหมดเพื่อความเนียน)
        if (scoreText != null)
        {
            StartCoroutine(ScoreAnimationRoutine(currentScore, bonusAdded, finalTotalScore));
        }

        // ==================== ✂️ จบการแก้ตรงนี้ ✂️ ====================
        
        if (nextLevelButton != null) nextLevelButton.SetActive(!isFinalLevel);
        if (mainMenuButton != null) mainMenuButton.SetActive(true);

        Time.timeScale = 0f;
    }

    // ระบบแอนิเมชันตอนจบ (ScoreAnimationRoutine / FinalLevelAnimationRoutine)
    // ตัวเลขคะแนนจะค่อยๆ วิ่งขึ้น (Count Up) โดยใช้คำสั่ง Mathf.Lerp
    // มีการไล่เฉดสีตัวเลขตามเกรดคะแนน และทำเอฟเฟกต์เฟดข้อความโบนัส
    /*/ มีระบบ "ตัดเกรด" (S, A, B, C, F) และใช้ PulseTextRoutine 
    ทำให้ตัวอักษรเกรดค่อยๆ สว่างและมืดสลับกัน (เหมือนกำลังหายใจ) ดูลื่นไหลกว่าการกระพริบแบบแข็งๆ /*/
    
    // 🎬 ฟังก์ชันแอนิเมชั่น (รวมร่าง!)
    IEnumerator ScoreAnimationRoutine(int startScore, int bonus, int targetScore)
    {
        // ------------------------------------------------------------------
        // ✅ STEP 1: ถ้าคะแนนเต็ม 100 หรือ ไม่มีโบนัส -> โชว์นิ่งๆ
        // ------------------------------------------------------------------
        if (startScore >= 100 || bonus <= 0)
        {
            Color scoreColor = (targetScore >= 100) ? Color.green : Color.white;
            string hexColor = ColorUtility.ToHtmlStringRGB(scoreColor);
            scoreText.text = $"TRAFFIC SCORE: <color=#{hexColor}>{targetScore}</color>/100";
            
            // ถ้าเป็นด่านสุดท้ายให้รอนิดนึงก่อนไปตัดเกรด
            if (isFinalLevel) yield return new WaitForSecondsRealtime(2.0f); 
        }
        else
        {
            // ------------------------------------------------------------------
            // ✅ STEP 2: มีโบนัส -> เล่นแอนิเมชั่นเลขวิ่ง (เหมือนกันทุกด่าน)
            // ------------------------------------------------------------------
            
            // Phase A: โชว์ค่าเริ่มต้น
            scoreText.text = $"TRAFFIC SCORE: {startScore}/100 <color=#FFFF00>(+{bonus} Bonus!)</color>";
            yield return new WaitForSecondsRealtime(1.5f);

            // Phase B: เลขวิ่ง + ดูดโบนัส
            float duration = 2.0f; 
            float timer = 0f;
            Color startColor = Color.white;
            
            // ปลายทาง: ถ้าเต็ม 100 เป็นเขียว, ถ้าไม่เต็มเป็นขาว (หรือจะใช้สีตามเกรดก็ได้)
            Color targetColor = (targetScore >= 100) ? Color.green : Color.white; 
            if (isFinalLevel) targetColor = GetGradeColor(targetScore); // ด่านจบใช้สีตามเกรด

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(timer / duration);

                int currentDisplay = (int)Mathf.Lerp(startScore, targetScore, progress);
                Color currentColor = Color.Lerp(startColor, targetColor, progress);
                string scoreHex = ColorUtility.ToHtmlStringRGB(currentColor);

                float alphaVal = Mathf.Lerp(1f, 0f, progress);
                int alphaInt = (int)(alphaVal * 255);
                string alphaHex = alphaInt.ToString("X2");
                
                scoreText.text = $"TRAFFIC SCORE: <color=#{scoreHex}>{currentDisplay}</color>/100 <color=#FFFF00{alphaHex}>(+{bonus} Bonus!)</color>";

                yield return null;
            }

            // จบ Loop: โชว์คะแนนสุทธิ
            string finalColorHex = ColorUtility.ToHtmlStringRGB(targetColor);
            scoreText.text = $"TRAFFIC SCORE: <color=#{finalColorHex}>{targetScore}</color>/100";
            
            if (isFinalLevel) yield return new WaitForSecondsRealtime(1.5f);
        }

        // ------------------------------------------------------------------
        // ✅ STEP 3: ทางแยกชะตากรรม (Final vs Normal)
        // ------------------------------------------------------------------
        
        if (isFinalLevel)
        {
            // --- ด่านสุดท้าย: เฟดหาย แล้วไปตัดเกรด ---
            for (float t = 1; t > 0; t -= Time.unscaledDeltaTime * 2) { scoreText.alpha = t; yield return null; }
            scoreText.text = ""; scoreText.alpha = 1; 

            // คำนวณเกรด
            string gradeText = "PUBLIC DANGER (F)";
            Color gradeColor = GetGradeColor(targetScore);
            if (targetScore == 100) gradeText = "LEGENDARY (S)";
            else if (targetScore >= 80) gradeText = "EXPERT (A)";
            else if (targetScore >= 50) gradeText = "SAFE DRIVER (B)";
            else if (targetScore >= 25) gradeText = "ROOKIE (C)";

            string hexGradeColor = ColorUtility.ToHtmlStringRGB(gradeColor);
            scoreText.text = $"<color=#{hexGradeColor}>{gradeText}</color> <size=70%>({targetScore}/100)</size>";
            
            // เฟดเข้า
            scoreText.alpha = 0;
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime * 2) { scoreText.alpha = t; yield return null; }
            scoreText.alpha = 1;
            
            StartCoroutine(PulseTextRoutine(scoreText));
        }
        else
        {
            // --- ด่านปกติ: ค้างไว้เลย ไม่ต้องทำอะไรต่อ ---
            // (จบฟังก์ชันแค่นี้ ผู้เล่นเห็น Traffic Score: 100/100 ค้างไว้สวยๆ)
        }
    }

    // 🎬 ฟังก์ชันใหม่: แอนิเมชั่นระดับ Cinematic สำหรับด่านจบ
    IEnumerator FinalLevelAnimationRoutine(int startScore, int bonus, int targetScore)
    {
        // ------------------------------------------------------------------
        // ✅ CASE 1: คะแนนเต็ม 100 หรือ ไม่มีโบนัส (+0)
        // ------------------------------------------------------------------
        if (startScore >= 100 || bonus <= 0)
        {
            // ไม่ต้องโชว์ (+0 Bonus) ให้รกตา
            // แสดงคะแนนปัจจุบันนิ่งๆ ไปเลย
            Color scoreColor = (startScore >= 100) ? Color.green : Color.white;
            string hexColor = ColorUtility.ToHtmlStringRGB(scoreColor);
            
            scoreText.text = $"TRAFFIC SCORE: <color=#{hexColor}>{startScore}</color>/100";
            yield return new WaitForSecondsRealtime(2.0f); // แช่นิ่งๆ 2 วิ
        }
        else
        {
            // ------------------------------------------------------------------
            // ✅ CASE 2: มีโบนัสให้โชว์ (Animation)
            // ------------------------------------------------------------------
            
            // Phase 1: โชว์ค่าเริ่มต้นพร้อมโบนัส
            scoreText.text = $"TRAFFIC SCORE: {startScore}/100 <color=#FFFF00>(+{bonus} Bonus!)</color>";
            yield return new WaitForSecondsRealtime(1.5f);

            // Phase 2: ดูดโบนัส! (Bonus Fade Out + Score Count Up)
            float duration = 2.0f; 
            float timer = 0f;
            Color startColor = Color.white;
            Color targetColor = GetGradeColor(targetScore);

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(timer / duration);

                // A. ตัวเลขวิ่ง
                int currentDisplay = (int)Mathf.Lerp(startScore, targetScore, progress);

                // B. สีตัวเลขเปลี่ยน
                Color currentColor = Color.Lerp(startColor, targetColor, progress);
                string scoreHex = ColorUtility.ToHtmlStringRGB(currentColor);

                // C. Alpha ของ Bonus (ลดจาก 1 -> 0)
                float alphaVal = Mathf.Lerp(1f, 0f, progress);
                int alphaInt = (int)(alphaVal * 255);
                string alphaHex = alphaInt.ToString("X2");
                
                // ประกอบร่าง
                scoreText.text = $"TRAFFIC SCORE: <color=#{scoreHex}>{currentDisplay}</color>/100 <color=#FFFF00{alphaHex}>(+{bonus} Bonus!)</color>";

                yield return null;
            }

            // จบ Loop: เคลียร์เศษ
            string finalColorHex = ColorUtility.ToHtmlStringRGB(targetColor);
            scoreText.text = $"TRAFFIC SCORE: <color=#{finalColorHex}>{targetScore}</color>/100";
            yield return new WaitForSecondsRealtime(1.5f); 
        }

        // ------------------------------------------------------------------
        // ✅ Phase 3: ตัดเกรด (Transition to Rank)
        // ------------------------------------------------------------------
        
        // 1. เฟดคะแนนชุดเก่าหายไป (Fade Out)
        for (float t = 1; t > 0; t -= Time.unscaledDeltaTime * 2)
        {
            scoreText.alpha = t;
            yield return null;
        }
        scoreText.text = ""; 
        scoreText.alpha = 1; 

        // 2. เตรียมข้อความ Rank
        string gradeText = "PUBLIC DANGER (F)";
        Color gradeColor = GetGradeColor(targetScore);

        if (targetScore == 100) gradeText = "LEGENDARY (S)";
        else if (targetScore >= 80) gradeText = "EXPERT (A)";
        else if (targetScore >= 50) gradeText = "SAFE DRIVER (B)";
        else if (targetScore >= 25) gradeText = "ROOKIE (C)";

        string hexGradeColor = ColorUtility.ToHtmlStringRGB(gradeColor);
        
        scoreText.text = $"<color=#{hexGradeColor}>{gradeText}</color> <size=70%>({targetScore}/100)</size>";
        
        // 3. เฟดข้อความ Rank เข้ามา (Fade In)
        scoreText.alpha = 0;
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime * 2)
        {
            scoreText.alpha = t;
            yield return null;
        }
        scoreText.alpha = 1;

        // 4. สั่งให้หายใจนุ่มๆ (แทนการกระพริบ)
        StartCoroutine(PulseTextRoutine(scoreText));
    }

    // ✅ ฟังก์ชันช่วยเลือกสี (เหมือนเดิม)
    Color GetGradeColor(int score)
    {
        if (score == 100) return Color.green; // S
        if (score >= 80) return new Color(0.7f, 1f, 0f); // A (Lime)
        if (score >= 50) return new Color(1f, 0.9f, 0f); // B (Amber)
        if (score >= 25) return new Color(1f, 0.6f, 0f); // C (Orange)
        return Color.red; // F
    }

    // ✅ ฟังก์ชันใหม่: "หายใจ" (Breathing/Pulse) แทนการกระพริบ
    IEnumerator PulseTextRoutine(TextMeshProUGUI targetText)
    {
        while (true)
        {
            // ค่อยๆ จางลง (จาก 1.0 -> 0.3)
            for (float t = 0; t < 1.0f; t += Time.unscaledDeltaTime) // ใช้เวลา 1 วิ
            {
                targetText.alpha = Mathf.Lerp(1.0f, 0.3f, t); 
                yield return null;
            }

            // ค่อยๆ สว่างขึ้น (จาก 0.3 -> 1.0)
            for (float t = 0; t < 1.0f; t += Time.unscaledDeltaTime) // ใช้เวลา 1 วิ
            {
                targetText.alpha = Mathf.Lerp(0.3f, 1.0f, t);
                yield return null;
            }
        }
    }

    // ✅ ฟังก์ชันใหม่: Fade เฉพาะคำว่า BONUS (ตัวหน้าอยู่นิ่งๆ)
    IEnumerator BlinkBonusTextRoutine(TextMeshProUGUI target, string baseText, string bonusText)
    {
        while (true)
        {
            // ค่อยๆ จางหาย (Fade Out)
            for (float t = 1; t >= 0.2f; t -= Time.unscaledDeltaTime * 1.5f)
            {
                // แปลงค่า 0-1 เป็น Hex Alpha (เช่น FF, AA, 00)
                byte alphaByte = (byte)(t * 255);
                string alphaHex = alphaByte.ToString("X2");

                // ประกอบร่าง: ข้อความหลัก + โบนัสสีทองที่เปลี่ยน Alpha ได้
                target.text = $"{baseText} <color=#FFD700><alpha=#{alphaHex}>{bonusText}</color>";
                yield return null;
            }

            // ค่อยๆ ชัดขึ้น (Fade In)
            for (float t = 0.2f; t <= 1; t += Time.unscaledDeltaTime * 1.5f)
            {
                byte alphaByte = (byte)(t * 255);
                string alphaHex = alphaByte.ToString("X2");
                target.text = $"{baseText} <color=#FFD700><alpha=#{alphaHex}>{bonusText}</color>";
                yield return null;
            }
        }
    }

    // (ฟังก์ชัน BlinkTextRoutine อันเดิม เก็บไว้ใช้กรณีไม่มีโบนัส)
    IEnumerator BlinkTextRoutine(TextMeshProUGUI targetText)
    {
        while (true) { targetText.alpha = 1f; yield return new WaitForSecondsRealtime(1.5f); targetText.alpha = 0f; yield return new WaitForSecondsRealtime(1.5f); }
    }
}
