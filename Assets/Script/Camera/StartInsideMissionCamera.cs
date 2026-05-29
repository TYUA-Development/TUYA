using UnityEngine;

public class StartInsideMissionCamera : MonoBehaviour
{
    public Vector3 targetPos;
    public float finalZoomSize = 35f;

    [Tooltip("카메라 Y를 플레이어 Y에 맞출지")]
    public bool followPlayerY = true;

    private Transform player;
    private GameObject cameraRig;
    private Camera targetCamera;
    private Collider2D areaCollider;

    private float originZoomSize;
    private float originCameraY;

    private bool isCameraControl;

    private void Start()
    {
        cameraRig = CameraMovement.Instance.gameObject;
        targetCamera = Camera.main;
        areaCollider = GetComponent<Collider2D>();

        originZoomSize = targetCamera.fieldOfView;
        originCameraY = cameraRig.transform.position.y;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;

            if (areaCollider.bounds.Contains(player.position))
            {
                StartCameraControl();
            }
        }
    }

    private void Update()
    {
        if (!isCameraControl || player == null)
            return;

        ControlCamera();
    }

    private void ControlCamera()
    {
        Bounds bounds = areaCollider.bounds;

        float centerX = bounds.center.x;

        float leftEdgeX = bounds.min.x;
        float rightEdgeX = bounds.max.x;

        Vector3 missionPos = targetPos;

        if (followPlayerY)
            missionPos.y = player.position.y;

        Vector3 leftOriginPos = new Vector3(
            leftEdgeX,
            originCameraY,
            cameraRig.transform.position.z
        );

        Vector3 rightOriginPos = new Vector3(
            rightEdgeX,
            originCameraY,
            cameraRig.transform.position.z
        );

        float playerX = player.position.x;

        Vector3 originPos;
        float t;

        if (playerX < centerX)
        {
            originPos = leftOriginPos;
            t = Mathf.InverseLerp(leftEdgeX, centerX, playerX);
        }
        else
        {
            originPos = rightOriginPos;
            t = Mathf.InverseLerp(rightEdgeX, centerX, playerX);
        }

        t = Mathf.Clamp01(t);
        t = Mathf.SmoothStep(0f, 1f, t);

        cameraRig.transform.position = Vector3.Lerp(originPos, missionPos, t);
        targetCamera.fieldOfView = Mathf.Lerp(originZoomSize, finalZoomSize, t);
    }

    private void StartCameraControl()
    {
        isCameraControl = true;
        CameraMovement.Instance.isMovingEvent = true;
    }

    private void StopCameraControl()
    {
        isCameraControl = false;

        CameraMovement.Instance.isMovingEvent = false;
        targetCamera.fieldOfView = originZoomSize;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        player = collision.transform;
        StartCameraControl();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        StopCameraControl();
        player = null;
    }
}