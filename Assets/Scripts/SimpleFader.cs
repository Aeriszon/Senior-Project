using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SimpleFader : MonoBehaviour
{
    public float fadeTime = 1.0f; // อยากให้นานแค่ไหน (1 วิ กำลังสวย)
    private Image fadePanel;

    void Start()
    {
        fadePanel = GetComponent<Image>();
        if (fadePanel != null)
        {
            // เริ่มมาสั่งให้มืดตึ๊บก่อน (กันเหนียว)
            Color c = fadePanel.color;
            c.a = 1f; 
            fadePanel.color = c;

            // สั่งให้ค่อยๆ จางหายไป
            StartCoroutine(FadeOutBlack());
        }
    }

    IEnumerator FadeOutBlack()
    {
        float elapsedTime = 0f;
        Color c = fadePanel.color;

        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            // คำนวณค่า Alpha จาก 1 -> 0
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeTime);
            
            c.a = alpha;
            fadePanel.color = c;
            
            yield return null;
        }

        // จบแล้วปิดทิ้งไปเลย จะได้ไม่กินสเปค
        gameObject.SetActive(false);
    }
}