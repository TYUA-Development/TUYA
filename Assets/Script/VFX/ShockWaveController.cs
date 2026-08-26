using System.Collections;
using UnityEngine;

public class ShockWaveController : MonoBehaviour
{
    public float duration = 0.5f;
    public Material material;

    [Header("World-space Origin")]
    [Tooltip("물결이 퍼져나가는 기준점. 비워두면 이 오브젝트의 위치를 사용합니다.")]
    public Transform origin;

    [Header("Image Reveal")]
    [Tooltip("물결이 이 거리(월드 단위)까지 퍼졌을 때 완전히 새 이미지로 교체됩니다.")]
    public float maxWorldRadius = 15f;

    private static readonly int WaveDistanceID = Shader.PropertyToID("_WaveDistance");
    private static readonly int FocalPointID = Shader.PropertyToID("_FocalPoint");
    private static readonly int ShockWaveOriginWSID = Shader.PropertyToID("_ShockWaveOriginWS");
    private static readonly int ShockWaveRadiusWSID = Shader.PropertyToID("_ShockWaveRadiusWS");

    private Coroutine shockWaveRoutine;

    private void Awake()
    {
        if (origin == null)
        {
            origin = transform;
        }

        if (material != null)
        {
            material.SetFloat(WaveDistanceID, -0.1f);
        }

        Shader.SetGlobalVector(ShockWaveOriginWSID, origin.position);
        Shader.SetGlobalFloat(ShockWaveRadiusWSID, 0f);
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
        Vector3 originPosition = origin.position;
        Camera cam = Camera.main;

        Shader.SetGlobalVector(ShockWaveOriginWSID, originPosition);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (material != null)
            {
                material.SetFloat(WaveDistanceID, Mathf.Lerp(-0.1f, 1f, t));

                if (cam != null)
                {
                    Vector3 viewportPoint = cam.WorldToViewportPoint(originPosition);
                    material.SetVector(FocalPointID, new Vector4(viewportPoint.x, viewportPoint.y, 0f, 0f));
                }
            }

            Shader.SetGlobalFloat(ShockWaveRadiusWSID, Mathf.Lerp(0f, maxWorldRadius, t));
            yield return null;
        }

        if (material != null)
        {
            material.SetFloat(WaveDistanceID, -0.1f);
        }

        Shader.SetGlobalFloat(ShockWaveRadiusWSID, maxWorldRadius);
        shockWaveRoutine = null;
    }
}
