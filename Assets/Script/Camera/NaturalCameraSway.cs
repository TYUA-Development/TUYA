using UnityEngine;

public class NaturalCameraSway : MonoBehaviour
{
    [Header("Position Sway")]
    public bool usePositionSway = true;

    [Tooltip("카메라 위치 흔들림 크기. 캐릭터가 작으면 0.05~0.12까지 올려도 됨")]
    public Vector2 positionAmount = new Vector2(0.05f, 0.025f);

    [Tooltip("위치 흔들림 속도. 낮을수록 천천히 흔들림")]
    public float positionFrequency = 0.35f;

    [Header("Rotation Sway")]
    public bool useRotationSway = true;

    [Tooltip("카메라 회전 흔들림 각도. 너무 높이면 멀미남")]
    public float rotationAmount = 0.08f;

    [Tooltip("회전 흔들림 속도")]
    public float rotationFrequency = 0.25f;

    [Header("Options")]
    public bool randomizeSeed = true;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;

    private float seedX;
    private float seedY;
    private float seedRot;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;

        if (randomizeSeed)
        {
            seedX = Random.Range(0f, 1000f);
            seedY = Random.Range(0f, 1000f);
            seedRot = Random.Range(0f, 1000f);
        }
        else
        {
            seedX = 12.34f;
            seedY = 56.78f;
            seedRot = 91.23f;
        }
    }

    private void LateUpdate()
    {
        float time = Time.time;

        Vector3 positionOffset = Vector3.zero;

        if (usePositionSway)
        {
            float noiseX = Mathf.PerlinNoise(seedX, time * positionFrequency) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(seedY, time * positionFrequency) * 2f - 1f;

            positionOffset = new Vector3(
                noiseX * positionAmount.x,
                noiseY * positionAmount.y,
                0f
            );
        }

        float rotationOffset = 0f;

        if (useRotationSway)
        {
            float noiseRot = Mathf.PerlinNoise(seedRot, time * rotationFrequency) * 2f - 1f;
            rotationOffset = noiseRot * rotationAmount;
        }

        transform.localPosition = baseLocalPosition + positionOffset;
        transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, rotationOffset);
    }

    public void ResetBaseTransform()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
    }
}