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

        // z는 건드리지 않는다 - 자식 하늘 레이어들의 localPosition.z가 서로 다른 깊이
        // 오프셋으로 쓰이는데, 부모 z 스케일까지 줄어들면 그 오프셋도 같이 줄어들어
        // Perspective 카메라 기준 자식들이 실제보다 훨씬 가까이 끌려와 버린다.
        skyImage.localScale = new Vector3(
            originScale.x * scaleRatio,
            originScale.y * scaleRatio,
            originScale.z
        );
    }
}