using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ShockWaveController : MonoBehaviour
{
    public float duration = 0.5f;

    private Material material;
    private int waveDistanceID;
    private Coroutine shockWaveRoutine;

    private void Awake()
    {
        material = GetComponent<SpriteRenderer>().material;
        waveDistanceID = Shader.PropertyToID("_WaveDistance");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            TriggerShockWave();
        }
    }

    public void TriggerShockWave()
    {
        if (shockWaveRoutine != null)
        {
            StopCoroutine(shockWaveRoutine);
        }
        shockWaveRoutine = StartCoroutine(ShockWaveRoutine());
    }

    private IEnumerator ShockWaveRoutine()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            material.SetFloat(waveDistanceID, Mathf.Lerp(-0.1f, 1f, t));
            yield return null;
        }
        material.SetFloat(waveDistanceID, 1f);
        shockWaveRoutine = null;
    }
}
