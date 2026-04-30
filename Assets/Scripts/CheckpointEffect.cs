using UnityEngine;

public class CheckpointEffect : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotateSpeed = 50f;

    void Update()
    {
        // สั่งให้หมุนรอบตัวเอง (แกน Y)
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
    }
}