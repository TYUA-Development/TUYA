using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class PerlinNoise : MonoBehaviour
{
    [Tooltip("떨림 속도")]
    public float frequency;

    [Tooltip("떨림 폭")]
    public float amplitude;

    [Tooltip("노이즈 감소 시간")]
    public float lerpTime = 1f;

    [Tooltip("노이즈 감소 폭 : 1부터 시작")]
    public float power;

    private float elapsed;
    private bool isPlaying;

    public void Play()
    {
        elapsed = 0f;
        isPlaying = true;
    }

    public Vector3 LerpNoise()
    {
        if (!isPlaying)
            return Vector3.zero;

        elapsed += Time.deltaTime * power;

        float t = Mathf.Clamp01(elapsed / lerpTime);

        // 점점 약해짐
        float fade = 1f - Mathf.SmoothStep(0f, 1f, t);

        float noiseX =
            (Mathf.PerlinNoise(Time.time * frequency, 0f) - 0.5f)
            * amplitude
            * fade;

        float noiseY =
            (Mathf.PerlinNoise(0f, Time.time * frequency) - 0.5f)
            * amplitude
            * fade;

        if (t >= 1f)
            isPlaying = false;

        return new Vector3(noiseX, 0f, 0f);
    }

    public void noise()
    {
        float noiseX = Mathf.PerlinNoise(Time.time * frequency, 0) - 0.5f;
        float noiseY = Mathf.PerlinNoise(0, Time.time * frequency) - 0.5f;

        Vector3 offset = new Vector3(noiseX, noiseY, 0) * amplitude;

        transform.position = transform.position + offset;
    }

}
