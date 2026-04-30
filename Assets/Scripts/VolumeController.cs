using UnityEngine;
using UnityEngine.UI;
using System.Collections; // ✅ ต้องมีบรรทัดนี้เพื่อใช้ตัวหน่วงเวลา

public class VolumeController : MonoBehaviour
{
    [Header("--- ตั้งค่าการเล่นเสียง ---")]
    [Tooltip("ติ๊กถูก = ค่อยๆ ดัง (เหมาะกับฉากเกม), ไม่ติ๊ก = ดังทันที (เหมาะกับ Menu)")]
    public bool useFadeIn = true; // ✅ เพิ่มตัวนี้ครับ

    [Header("--- ส่วนที่ต้องลากของใน Scene มาใส่ ---")]
    public Image[] volumeBoxes;     // ลากกล่อง 10 อันมาใส่เหมือนเดิม
    public Image speakerIconImage;  // ลากตัว "Object รูปลำโพง" ใน Canvas มาใส่

    [Header("--- ส่วนที่ต้องลากไฟล์รูปจาก Project มาใส่ ---")]
    public Sprite activeSprite;     // ลากไฟล์รูป "ลำโพงปกติ" มาใส่
    public Sprite mutedSprite;      // ลากไฟล์รูป "ลำโพงกากบาท" มาใส่

    // --------------- ส่วนที่เพิ่มเข้ามาใหม่ ---------------
    [Header("Icon Scale Settings")]
    [Range(0.1f, 1.5f)] 
    public float activeScale = 1.0f; // <--- ขนาดตอนเปิดเสียง (ปรับเลขนี้ถ้ารูปมันใหญ่ไป)
    [Range(0.1f, 1.5f)]
    public float mutedScale = 1.0f;  // <--- ขนาดตอนปิดเสียง
    // --------------------------------------------------

    [Header("--- ตั้งค่าสี ---")]
    public Color filledColor = Color.white;
    public Color emptyColor = new Color(1, 1, 1, 0.3f); // สีจางๆ

    // ตัวแปรจำค่า (ไม่ต้องไปยุ่งกับมัน)
    private int currentLevel = 5;
    private int lastLevel = 5;      // ตัวจำค่าเดิมก่อน Mute

    // ชื่อตัวแปรสำหรับเซฟลงเครื่อง (Key) - เพิ่มบรรทัดนี้เข้าไปครับ!
    private const string SaveKey = "GameVolumeLevel";
    
    // ✅ 1. เพิ่ม Awake: สั่งใบ้เสียงทันทีที่วัตถุเกิด (กันเสียงลอด 100%)
    void Awake()
    {
        // ถ้าจะใช้ Fade ก็ใบ้กินก่อน (กันเสียงลอด)
        // แต่ถ้าไม่ใช้ (อยากให้ดังเลย) ก็ไม่ต้องไปยุ่งกับมัน
        if (useFadeIn)
        {
            AudioListener.volume = 0f; 
        }
    }

    // ✅ เปลี่ยน Start ธรรมดา เป็น IEnumerator (เพื่อสั่งรอได้)
    IEnumerator Start()
    {
        // 1. โหลดค่ามาก่อน (เพื่ออัปเดต UI ให้ตรงก่อน)
        int savedLevel = 10;
        if (PlayerPrefs.HasKey(SaveKey))
        {
            savedLevel = PlayerPrefs.GetInt(SaveKey);
            currentLevel = savedLevel;
            if (savedLevel > 0) lastLevel = savedLevel;
        }
        // อัปเดต UI ทันที (หลอดไฟจะได้ขึ้นรอไว้เลย)
        UpdateUI(savedLevel);

        // คำนวณความดังเป้าหมาย (Target Volume)
        float targetVolume = Mathf.Pow((float)savedLevel / 10f, 2);

        // ✅ เช็คตรงนี้: ถ้าไม่เอา Fade ก็ตั้งเสียงเลยจบๆ
        if (!useFadeIn)
        {
            AudioListener.volume = targetVolume;
            yield break; // จบการทำงานทันที
        }

        // ============================================================
        // 🔥 ระบบ Fade In: ค่อยๆ เร่งเสียงจาก 0 -> เป้าหมาย ใน 0.5 วินาที
        // ============================================================
        float duration = 0.5f; // ระยะเวลา Fade (ปรับให้นานขึ้นได้ถ้าชอบนวลๆ)
        float currentTime = 0f;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            // ค่อยๆ ขยับเสียงขึ้นทีละนิด
            AudioListener.volume = Mathf.Lerp(0f, targetVolume, currentTime / duration);
            yield return null; // รอเฟรมถัดไป
        }

        // จบข่าว: ล็อกเสียงไว้ที่เป้าหมายเป๊ะๆ
        AudioListener.volume = targetVolume;
    }

    // ✅ 2. เพิ่ม OnEnable: เวลาเมนูเด้งขึ้นมา ให้วาดหลอดไฟใหม่ (แก้บั๊กหลอดไฟเพี้ยน)
    void OnEnable()
    {
        // ถ้าเกมเริ่มไปแล้ว (ไม่ใช่เฟรมแรก) ให้ใช้ค่าปัจจุบันวาดหลอดไฟ
        if (Time.timeSinceLevelLoad > 0.1f)
        {
            UpdateUI(currentLevel);
        }
    }

    // ฟังก์ชันนี้เอาไว้ให้ปุ่มลำโพงกด (Toggle)
    public void ToggleMute()
    {
        if (currentLevel > 0) SetVolume(0);
        else SetVolume(lastLevel > 0 ? lastLevel : 1);
    }

    // ฟังก์ชันหลักที่ปุ่ม 1-10 จะเรียกใช้
    public void SetVolume(int level)
    {
        // เซฟค่า
        PlayerPrefs.SetInt(SaveKey, level);
        PlayerPrefs.Save(); 

        ApplyVolumeSettings(level);
    }

    void ApplyVolumeSettings(int level)
    {
        currentLevel = level;
        if (level > 0) lastLevel = level;

        // สูตรเสียง (Mathf.Pow)
        float realVolume = Mathf.Pow((float)level / 10f, 2);
        AudioListener.volume = realVolume;

        UpdateUI(level);
    }
    void UpdateUI(int level)
    {
        if (speakerIconImage != null) 
        {
            if (level > 0)
            {
                speakerIconImage.sprite = activeSprite;
                speakerIconImage.transform.localScale = new Vector3(activeScale, activeScale, 1f);
            }
            else
            {
                speakerIconImage.sprite = mutedSprite;
                speakerIconImage.transform.localScale = new Vector3(mutedScale, mutedScale, 1f);
            }
        }
        
        // // ✅ 3. แก้สูตรคำนวณเสียง (ใช้ Mathf.Pow)
        // // เพื่อให้ระดับ 1 เบาเงียบกริบจริงๆ (ถ้าใช้แบบเดิม /10f มันจะดังไป)
        // AudioListener.volume = Mathf.Pow((float)level / 10f, 2);

        // --- Logic เปลี่ยนสีหลอดไฟ 10 ช่อง (อันเดิม) ---
        if (volumeBoxes != null)
        {
            for (int i = 0; i < volumeBoxes.Length; i++)
            {
                if (volumeBoxes[i] == null) continue; 

                if (i < level)
                    volumeBoxes[i].color = filledColor;
                else
                    volumeBoxes[i].color = emptyColor;
            }
        }
    }
}
