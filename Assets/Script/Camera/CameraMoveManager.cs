using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct CameraMoveInfo
{
    public bool followPlayer;

    public Vector3 startPos;
    public Vector3 endPos;
    public Vector3 targetPos;

    // 0 이하라면 줌 변경 안 함
    public float zoom;
}

public class CameraMoveManager : MonoBehaviour
{
    [Header("Camera Move Infos")]
    public List<CameraMoveInfo> list = new List<CameraMoveInfo>();

    public GameObject targetCameraRig;
    private Camera cameraComponent;

    [SerializeField] public PlayerController player;
    private CameraMovement cameraMovement;

    [SerializeField] private int currentIndex = 0;

    private float defaultFov;

    private void Awake()
    {
        if(targetCameraRig != null)
            cameraMovement = targetCameraRig.GetComponent<CameraMovement>();

        cameraComponent = Camera.main;

        if (targetCameraRig != null)
            defaultFov = cameraComponent.fieldOfView;
    }

    private void Update()
    {
        if (targetCameraRig == null)
            return;

        if (currentIndex >= list.Count)
            return;

        CameraMoveInfo info = list[currentIndex];
        Vector3 cameraPos = player.transform.position;

        ApplyCameraMoveInfo(info, cameraPos);

        if (HasPassedEndPos(cameraPos, info.startPos, info.endPos))
        {
            FinishCurrentInfo(info);
            currentIndex++;
        }
    }

    private void ApplyCameraMoveInfo(CameraMoveInfo info, Vector3 cameraPos)
    {
        if (cameraMovement != null)
            cameraMovement.enabled = info.followPlayer;

        if(!info.followPlayer)
        {
            cameraMovement.enabled = false;
            CameraMovement.Instance.MoveCamera(info.targetPos);
        }
        else
        {
            cameraMovement.enabled = true;
        }

        if (info.zoom > 0f)
        {
            float t = GetProgress(cameraPos, info.startPos, info.endPos);

            cameraComponent.fieldOfView = Mathf.Lerp(
                defaultFov,
                info.zoom,
                t
            );
        }
    }

    private void FinishCurrentInfo(CameraMoveInfo info)
    {
        if (info.zoom > 0f)
        {
            cameraComponent.fieldOfView = info.zoom;
            defaultFov = info.zoom;
        }
    }

    private float GetProgress(Vector3 pos, Vector3 start, Vector3 end)
    {
        Vector2 start2D = new Vector2(start.x, start.y);
        Vector2 end2D = new Vector2(end.x, end.y);
        Vector2 pos2D = new Vector2(pos.x, pos.y);

        Vector2 startToEnd = end2D - start2D;
        Vector2 startToPos = pos2D - start2D;

        float lengthSqr = startToEnd.sqrMagnitude;

        if (lengthSqr <= 0.0001f)
            return 1f;

        float t = Vector2.Dot(startToPos, startToEnd) / lengthSqr;

        return Mathf.Clamp01(t);
    }

    private bool HasPassedEndPos(Vector3 pos, Vector3 start, Vector3 end)
    {
        Vector2 start2D = new Vector2(start.x, start.y);
        Vector2 end2D = new Vector2(end.x, end.y);
        Vector2 pos2D = new Vector2(pos.x, pos.y);

        Vector2 startToEnd = end2D - start2D;
        Vector2 startToPos = pos2D - start2D;

        float lengthSqr = startToEnd.sqrMagnitude;

        if (lengthSqr <= 0.0001f)
            return true;

        float t = Vector2.Dot(startToPos, startToEnd) / lengthSqr;

        return t >= 1f;
    }
}
