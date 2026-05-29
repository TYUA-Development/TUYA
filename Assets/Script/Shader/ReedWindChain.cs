using UnityEngine;

public class ReedWindChain : MonoBehaviour
{
    [Header("Bones")]
    public Transform rootBone;
    public Transform midBone;
    public Transform topBone;

    [Header("Wind")]
    public float windAngle = 3f;
    public float windSpeed = 0.6f;

    [Header("Strength By Height")]
    public float rootStrength = 0f;
    public float midStrength = 0.35f;
    public float topStrength = 1f;

    private Quaternion rootStartRot;
    private Quaternion midStartRot;
    private Quaternion topStartRot;

    private float randomOffset;

    void Start()
    {
        if (rootBone != null)
            rootStartRot = rootBone.localRotation;

        if (midBone != null)
            midStartRot = midBone.localRotation;

        if (topBone != null)
            topStartRot = topBone.localRotation;

        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float wind = Mathf.Sin(Time.time * windSpeed + randomOffset);

        if (rootBone != null)
        {
            float angle = wind * windAngle * rootStrength;
            rootBone.localRotation = rootStartRot * Quaternion.Euler(0f, 0f, angle);
        }

        if (midBone != null)
        {
            float angle = wind * windAngle * midStrength;
            midBone.localRotation = midStartRot * Quaternion.Euler(0f, 0f, angle);
        }

        if (topBone != null)
        {
            float angle = wind * windAngle * topStrength;
            topBone.localRotation = topStartRot * Quaternion.Euler(0f, 0f, angle);
        }
    }
}