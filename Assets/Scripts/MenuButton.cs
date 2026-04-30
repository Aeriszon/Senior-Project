using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButton : MonoBehaviour
{
    public GameObject blackCurtain; // ลาก FadePanel มาใส่ตรงนี้

    public void PlayGame()
    {
        // 1. เปิดผ้าดำมาบังตาทันที! (ฉับ!)
        if (blackCurtain != null)
        {
            blackCurtain.SetActive(true);
        }

        // 2. โหลดด่าน 1 (เช็คเลข Index ใน Build Settings ด้วยนะ ปกติคือ 1)
        SceneManager.LoadScene(1);
    }
}
