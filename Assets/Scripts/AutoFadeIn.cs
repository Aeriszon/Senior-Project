using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AutoFadeIn : MonoBehaviour
{
    public float fadeTime = 1.5f; // เวลาในการค่อยๆ สว่าง (วินาที)
    private Image fadePanel;

    void Start()
    {
        fadePanel = GetComponent<Image>();
        if (fadePanel != null)
        {
            StartCoroutine(FadeOutBlack());
        }
    }

    IEnumerator FadeOutBlack()
    {
        float elapsedTime = 0f;
        Color c = fadePanel.color;
        
        // บังคับให้เริ่มที่ดำสนิทก่อน
        c.a = 1f; 
        fadePanel.color = c;

        // ค่อยๆ ลดค่า Alpha ลง
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeTime);
            c.a = alpha;
            fadePanel.color = c;
            yield return null;
        }

        // จางสุดแล้ว ปิดทิ้งไปเลย
        gameObject.SetActive(false);
    }
}
