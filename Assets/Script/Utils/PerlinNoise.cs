using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class PerlinNoise : MonoBehaviour
{
    [Tooltip("¶³¸² ¼Óµµ")]
    public float frequency;

    [Tooltip("¶³¸² Æø")]
    public float amplitude;

    public void noise()
    {
        float noiseX = Mathf.PerlinNoise(Time.time * frequency, 0) - 0.5f;
        float noiseY = Mathf.PerlinNoise(0, Time.time * frequency) - 0.5f;

        Vector3 offset = new Vector3(noiseX, noiseY, 0) * amplitude;

        transform.position = transform.position + offset;
    }

}
