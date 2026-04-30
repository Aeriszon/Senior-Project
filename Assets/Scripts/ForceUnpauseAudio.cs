using UnityEngine;

public class ForceUnpauseAudio : MonoBehaviour 
{
    void Awake() 
    {
        // สั่งให้ AudioSource ตัวนี้ เป็นอมตะ! (ดังทะลุ Pause)
        AudioSource audio = GetComponent<AudioSource>();
        if (audio != null)
        {
            audio.ignoreListenerPause = true; 
        }
    }
}
