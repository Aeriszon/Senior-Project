using UnityEngine;

public class MyCarSound : MonoBehaviour
{
    public float audioPitch = 1;
    AudioSource audioSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.pitch = audioPitch;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
