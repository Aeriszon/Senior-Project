using UnityEngine;

public class PlayerEngineSound : MonoBehaviour
{
    public Rigidbody carRigidbody;
    public AudioSource engineAudio;

    [Header("Pitch Settings")]
    public float minPitch = 0.8f;   // เสียงทุ้มต่ำตอนจอด (เสียงเดินเบา)
    public float maxPitch = 2.0f;   // เสียงแหลมตอนเหยียบมิด
    
    [Header("Volume Settings")]
    public float minVolume = 0.3f;  // ตอนจอด ให้เสียงเบาลงหน่อย (จะได้ไม่หนวกหู)
    public float maxVolume = 1.0f;  // ตอนวิ่ง ให้เสียงดังเต็มที่

    public float maxSpeed = 100f;

    void Update()
    {
        if (carRigidbody == null || engineAudio == null) return;

        float currentSpeed = carRigidbody.linearVelocity.magnitude * 3.6f;
        float speedRatio = currentSpeed / maxSpeed; // ค่า 0 ถึง 1

        // 1. ปรับ Pitch (เสียงสูง-ต่ำ)
        engineAudio.pitch = Mathf.Lerp(minPitch, maxPitch, speedRatio);

        // 2. ปรับ Volume (เสียงดัง-เบา) **เพิ่มส่วนนี้เข้าไป**
        // ยิ่งวิ่งเร็ว เสียงยิ่งดัง / จอดนิ่งๆ เสียงจะเบาลงเหลือแค่ minVolume
        engineAudio.volume = Mathf.Lerp(minVolume, maxVolume, speedRatio);
    }
}