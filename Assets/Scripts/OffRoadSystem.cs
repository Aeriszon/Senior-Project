using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OffRoadSystem : MonoBehaviour
{
    [Header("---- Game Settings ----")]
    public int countdownStart = 5;
    public float initialDelay = 1.0f;
    public string safeTag = "Road"; 
    
    [Header("---- Visual Settings ----")]
    [Range(0f, 1f)] public float maxRedAlpha = 0.15f; 
    public float blinkDuration = 1.0f;

    [Header("---- UI References ----")]
    public GameObject warningPanel;     
    public TMP_Text headerText;         
    public TMP_Text timerText;          
    public Image redOverlay;            

    [Header("---- Audio System ----")]
    public AudioSource uiAudioSource;   
    public AudioClip beepClip;          

    [Header("---- Sound Timing ----")]
    public float fastBeepThreshold = 2.0f;

    // Internal Vars
    private float timeSinceViolation = 0f;
    private float lastBeepTime = -1f;
    private bool hasEnteredRoad = false;
    private bool isOffRoad = false;
    private bool isGameOver = false;

    // เก็บ Reference รถไว้ปิดเสียง
    private Transform playerTransform; 

    void Start()
    {
        // หาตัวรถเตรียมไว้ (เอาไว้ปิดเสียงตอนตาย)
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;

        if (redOverlay != null)
        {
            RectTransform rt = redOverlay.GetComponent<RectTransform>();
            if (rt != null) {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }
        ResetSystem();
    }

    void Update()
    {
        if (isGameOver) return;

        // ✅ 1. เพิ่มบรรทัดนี้: ถ้า Manager บอกว่าจบ (ชนะ/แพ้) ให้เลิกทำงาน!
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.isGameOver) 
        {
            // ปิดป้ายเตือนทิ้งทันที (เผื่อค้างอยู่)
            if (warningPanel != null) warningPanel.SetActive(false);
            if (headerText != null) headerText.enabled = false;
            return; 
        }

        // ✅ แก้ตรงนี้: ถ้าเกม Pause อยู่ ห้ามทำงาน! (แก้ UI ซ้อน)
        if (Time.timeScale == 0) return;

        CheckGround(); 

        if (hasEnteredRoad && isOffRoad)
        {
            HandlePenalty();
        }
        else
        {
            ResetSystem();
        }
    }

    void CheckGround()
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(origin, Vector3.down, out hit, 3.0f))
        {
            if (hit.collider.CompareTag(safeTag))
            {
                hasEnteredRoad = true;
                isOffRoad = false;
            }
            else
            {
                isOffRoad = true;
            }
        }
        else 
        {
            isOffRoad = true;
        }
    }

    void HandlePenalty()
    {
        timeSinceViolation += Time.deltaTime;
        float gracePeriod = 0.5f;

        if (timeSinceViolation < gracePeriod) return; 

        if (warningPanel != null) warningPanel.SetActive(true);

        float adjustedTime = timeSinceViolation - gracePeriod;
        float blinkPhase = adjustedTime % blinkDuration;
        bool isVisible = blinkPhase < (blinkDuration * 0.5f);

        if (headerText != null) headerText.enabled = isVisible;
        if (redOverlay != null) SetRedAlpha(isVisible ? maxRedAlpha : 0f);

        float timerProgress = adjustedTime - initialDelay;

        if (timerProgress < 0)
        {
            if (timerText != null) timerText.text = ""; 
        }
        else
        {
            float timeLeft = countdownStart - timerProgress;
            int displayNum = Mathf.CeilToInt(timeLeft);

            if (timerText != null) timerText.text = displayNum.ToString();

            if (timeLeft <= 0)
            {
                TriggerGameOver();
                return;
            }

            HandleSound(timeLeft);
        }
    }

    void HandleSound(float timeLeft)
    {
        if (uiAudioSource == null || beepClip == null) return;

        float currentInterval = (timeLeft <= fastBeepThreshold) ? 0.2f : 1.0f;

        if (timeSinceViolation - lastBeepTime >= currentInterval)
        {
            uiAudioSource.PlayOneShot(beepClip);
            lastBeepTime = timeSinceViolation;
        }
    }

    void ResetSystem()
    {
        timeSinceViolation = 0f;
        lastBeepTime = -1f;

        if (warningPanel != null) warningPanel.SetActive(false);
        if (redOverlay != null) SetRedAlpha(0f);
        if (headerText != null) headerText.enabled = true;
    }

    void SetRedAlpha(float alpha)
    {
        if (redOverlay == null) return;
        Color c = redOverlay.color;
        c.a = alpha;
        redOverlay.color = c;
    }

    void TriggerGameOver()
    {
        Debug.Log("GAME OVER: Out of Area");
        isGameOver = true;

        if (warningPanel != null) warningPanel.SetActive(false);
        if (redOverlay != null) redOverlay.gameObject.SetActive(false);

        // ✅ เพิ่ม: ปิดเสียงรถที่ตัวรถโดยตรง (กันเหนียวเสียงค้าง 0km/h)
        if (playerTransform != null)
        {
            var carAudio = playerTransform.GetComponent<AudioSource>();
            if (carAudio != null)
            {
                carAudio.volume = 0f;
                carAudio.Stop();
                carAudio.enabled = false;
            }
        }

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.GameOver();
        }
        else
        {
            Time.timeScale = 0; 
        }
    }
}