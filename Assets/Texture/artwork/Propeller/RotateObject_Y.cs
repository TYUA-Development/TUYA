using UnityEngine;

public class RotateObject_Y : MonoBehaviour
{
    public float rotateSpeed = 200f;

    void Update()
    {
        transform.Rotate(0, rotateSpeed * Time.deltaTime, 0);
    }
}