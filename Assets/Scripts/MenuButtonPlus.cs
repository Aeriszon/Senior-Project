using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuButtonPlus : MonoBehaviour
{
    public AudioSource audioSource;

    void Start()
    {
        // 1. หาเสียงในตัวมันเอง
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        
        // 2. บังคับให้ดังทะลุ Pause (ทางลัดที่นายชอบ)
        if (audioSource != null) audioSource.ignoreListenerPause = true;
    }

    // เอาฟังก์ชันนี้ไปใส่ในปุ่ม On Click () แทนของเดิม
    public void GoToMenu()
    {
        StartCoroutine(ProcessClick());
    }

    IEnumerator ProcessClick()
    {
        // เล่นเสียง
        if (audioSource != null) audioSource.Play();
        
        // รอเสียงจบ (0.5 วิ) แบบไม่สน TimeScale
        yield return new WaitForSecondsRealtime(0.5f);

        // คืนเวลา แล้วย้ายฉาก
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
