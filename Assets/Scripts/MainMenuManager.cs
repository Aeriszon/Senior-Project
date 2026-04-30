using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // ✅ เพิ่มส่วนนี้: เพื่อให้แน่ใจว่ากลับมาหน้าเมนูแล้วเสียงดังปกติ + เวลาเดิน
    void Start()
    {
        Time.timeScale = 1f;           // คืนค่าเวลา (เผื่อหยุดไว้)
        AudioListener.pause = false;   // คืนค่าเสียง (สำคัญมาก!)
    }

    public void StartGame()
    {
        Debug.Log("Button Clicked!"); 
        SceneManager.LoadScene(1);     // โหลดฉากเล่นเกม
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
