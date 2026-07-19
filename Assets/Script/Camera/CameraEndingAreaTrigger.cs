using UnityEngine;

public class CameraEndingAreaTrigger : CameraRestoreAreaTrigger
{
    [Header("Ending - Fixed Height")]
    [SerializeField] private float fixedCameraY = 15.13f;

    protected override float GetTargetCameraY(Transform player, float currentY)
    {
        return fixedCameraY;
    }

    protected override void FinalizeCameraY(Transform player)
    {
        if (CameraMovement.Instance == null)
            return;

        CameraMovement.Instance.SetCameraRigY(fixedCameraY);
    }
}
