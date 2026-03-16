using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public GameObject Charactor;
    public float cameraHeight;

    // Update is called once per frame
    void Update()
    {
        Vector3 pos = new Vector3(Charactor.transform.position.x, cameraHeight, -10);
        transform.position = pos;
    }
}
