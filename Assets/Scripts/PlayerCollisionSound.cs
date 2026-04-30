using UnityEngine;

public class PlayerCollisionSound : MonoBehaviour
{
    public AudioSource crashAudio;
    public float minCrashForce = 10f; // ต้องชนแรงแค่ไหนเสียงถึงจะดัง (ลองปรับเลขนี้ดู)

    void OnCollisionEnter(Collision collision)
    {
        // เช็คความแรงของการชน (Relative Velocity)
        if (collision.relativeVelocity.magnitude > minCrashForce)
        {
            // สุ่ม Pitch นิดหน่อย ให้เสียงไม่ซ้ำซาก (เทคนิคโปร)
            crashAudio.pitch = Random.Range(0.8f, 1.2f);
            
            // สั่งเล่นเสียง (PlayOneShot ทับกันได้ถ้าชนรัวๆ)
            crashAudio.PlayOneShot(crashAudio.clip);
        }
    }
}
