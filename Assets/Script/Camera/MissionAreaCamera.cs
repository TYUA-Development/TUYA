using UnityEngine;

public class MissionAreaCamera : MonoBehaviour
{
    public Vector3 targetPos;

    [Tooltip("targetPos.x 기준 좌우 거리")]
    public float maxSizeXPos = 5f;

    public float finalZoomSize;

    [Tooltip("카메라와 바닥 기준 Z 거리")]
    public float groundDistance = 28f;

    private bool isCameraControl;

    private Transform player;
    private GameObject cameraRig;
    private Camera targetCamera;
    private Collider2D areaCollider;

    private Vector3 enterCameraPos;
    private Vector3 exitCameraPos;

    private float startZoomSize;
    private float startHalfHeight;

    private float enterX;
    private float exitX;

    private bool isLeftToRight;

    private void Start()
    {
        cameraRig = CameraMovement.Instance.gameObject;
        targetCamera = Camera.main;
        areaCollider = GetComponent<Collider2D>();

        enterX = areaCollider.bounds.min.x;
        exitX = areaCollider.bounds.max.x;
    }

    private void Update()
    {
        if (!isCameraControl || player == null)
            return;

        ControlCamera();
    }

    private void ControlCamera()
    {
        float playerX = player.position.x;

        float leftZoomEndX = targetPos.x - maxSizeXPos;
        float rightZoomStartX = targetPos.x + maxSizeXPos;
        targetPos.y = player.position.y + 15.13f;

        if (isLeftToRight)
        {
            if (playerX < leftZoomEndX)
            {
                float t = Mathf.InverseLerp(enterX, leftZoomEndX, playerX);
                ApplyCamera(Vector3.Lerp(enterCameraPos, targetPos, Smooth(t)), Smooth(t));
            }
            else if (playerX <= rightZoomStartX)
            {
                ApplyCamera(targetPos, 1f);
            }
            else
            {
                float t = Mathf.InverseLerp(rightZoomStartX, exitX, playerX);
                ApplyCamera(Vector3.Lerp(targetPos, exitCameraPos, Smooth(t)), 1f - Smooth(t));
            }
        }
        else
        {
            if (playerX > rightZoomStartX)
            {
                float t = Mathf.InverseLerp(exitX, rightZoomStartX, playerX);
                ApplyCamera(Vector3.Lerp(enterCameraPos, targetPos, Smooth(t)), Smooth(t));
            }
            else if (playerX >= leftZoomEndX)
            {
                ApplyCamera(targetPos, 1f);
            }
            else
            {
                float t = Mathf.InverseLerp(leftZoomEndX, enterX, playerX);
                ApplyCamera(Vector3.Lerp(targetPos, exitCameraPos, Smooth(t)), 1f - Smooth(t));
            }
        }
    }

    private float Smooth(float t)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
    }

    private void ApplyCamera(Vector3 basePos, float zoomT)
    {
        zoomT = Mathf.Clamp01(zoomT);

        float currentFov = Mathf.Lerp(startZoomSize, finalZoomSize, zoomT);
        targetCamera.fieldOfView = currentFov;

        float currentHalfHeight =
            groundDistance * Mathf.Tan(currentFov * 0.5f * Mathf.Deg2Rad);

        float yOffset = currentHalfHeight - startHalfHeight;

        //basePos.y += yOffset;
        cameraRig.transform.position = basePos;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        player = collision.transform;
        isCameraControl = true;

        enterCameraPos = cameraRig.transform.position;

        // 핵심: targetPos와 enterCameraPos의 간격만큼 targetPos 이후로 이동
        exitCameraPos = new Vector3(
    targetPos.x + (targetPos.x - enterCameraPos.x),
    enterCameraPos.y,
    enterCameraPos.z
);

        startZoomSize = targetCamera.fieldOfView;

        startHalfHeight =
            groundDistance * Mathf.Tan(startZoomSize * 0.5f * Mathf.Deg2Rad);

        isLeftToRight = player.position.x < targetPos.x;

        CameraMovement.Instance.isMovingEvent = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        isCameraControl = false;
        player = null;

        CameraMovement.Instance.isMovingEvent = false;
    }
}