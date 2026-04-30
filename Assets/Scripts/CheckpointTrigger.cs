using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    // ใช้ OnTriggerEnter เพื่อเช็คการชนเบื้องต้น
    private void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Player"))
    {
        // ต้องเรียก CheckpointRandomizer (ตัวที่มีอยู่ใน GameController)
        var manager = FindFirstObjectByType<CheckpointRandomizer>(); 
        
        if (manager != null)
        {
            manager.OnCheckpointCaptured();
            Debug.Log("ส่งสัญญาณไปที่ Randomizer สำเร็จ!");
        }
        else
        {
            Debug.LogError("หา CheckpointRandomizer ใน Scene ไม่เจอ! เช็คชื่อสคริปต์ที่ GameController ด้วย");
        }
    }
}
}