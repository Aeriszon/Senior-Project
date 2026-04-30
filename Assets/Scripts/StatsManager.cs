using UnityEngine;
using System.IO;  // จำเป็นสำหรับจัดการไฟล์
using System;     // จำเป็นสำหรับเรื่องวันเวลา

public class StatsManager : MonoBehaviour
{
    // Singleton: ให้เรียกใช้ได้ง่ายๆ จากทุกที่
    public static StatsManager Instance;

    [Header("File Settings")]
    public string fileName = "GameStats.csv"; // ชื่อไฟล์
    private string filePath;

    void Awake()
    {
        // Setup Singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Setup File Path (เซฟไว้ในโฟลเดอร์เดียวกับตัวเกม)
        // ถ้าเป็น Editor จะอยู่ที่โฟลเดอร์โปรเจกต์ (นอก Assets)
        filePath = Path.Combine(Application.dataPath, "../" + fileName);

        // สร้างไฟล์ + หัวตาราง (ถ้ายังไม่มี)
        if (!File.Exists(filePath))
        {
            // แก้ Header
            string header = "Date,Time,Level,Result,Score,RedLight,Speeding,Hit_Car,Hit_Building,Hit_Obstacle,Sidewalk,WrongWay\n";
            File.WriteAllText(filePath, header);
        }
    }

    // ฟังก์ชันพระเอก: เรียกตัวนี้เพื่อเซฟข้อมูล!
    public void SaveLog(string levelName, string resultStatus, int score, 
                        int redLight, int speeding, 
                        int hitCar, int hitBuilding, int hitObstacle, int sidewalk, int wrongWay)
    {
        // 1. ดึงวันเวลาปัจจุบัน
        string date = DateTime.Now.ToString("yyyy-MM-dd");
        string time = DateTime.Now.ToString("HH:mm:ss");

        // 2. จัดรูปแบบข้อความที่จะบันทึก (คั่นด้วยลูกน้ำ)
        // ระวัง! ลำดับต้องตรงกับ header ข้างบน
        string newLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}\n",
            date,       // 0
            time,       // 1
            levelName,  // 2
            resultStatus, // 3 (Pass/Fail/Restart/Quit)
            score,      // 4
            redLight,   // 5
            speeding,   // 6
            hitCar,     // 7
            hitBuilding,// 8
            hitObstacle,// 9
            sidewalk,   // 10
            wrongWay    // 11
        );

        // 3. เขียนต่อท้ายไฟล์เดิม (Append)
        try
        {
            File.AppendAllText(filePath, newLine);
            Debug.Log("✅ บันทึกสถิติลงไฟล์เรียบร้อย: " + filePath);
        }
        catch (Exception e)
        {
            Debug.LogError("❌ บันทึกไฟล์ไม่สำเร็จ: " + e.Message);
        }
    }
}
