using UnityEngine;

public class SkyZoomScaler : MonoBehaviour
{
    public Camera targetCamera;
    public Transform skyImage;

    private float originFov;
    private Vector3 originScale;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        originFov = targetCamera.fieldOfView;

        if (skyImage != null)
            originScale = skyImage.localScale;
    }

    void LateUpdate()
    {
        if (targetCamera == null || skyImage == null)
            return;

        float originHeight =
            Mathf.Tan(originFov * 0.5f * Mathf.Deg2Rad);

        float currentHeight =
            Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);

        float scaleRatio = currentHeight / originHeight;

        skyImage.localScale = originScale * scaleRatio;
    }
}