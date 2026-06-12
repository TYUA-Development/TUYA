using UnityEngine;

public class CircleHitObject : MonoBehaviour, IArrowHit
{
    [Header("Existing Stone Circle System")]
    public StoneCircleManager manager;
    private int triggerId;

    [Header("Special Machine Activation")]
    public WindMachineActivationController activationController;

    [Header("Hit Option")]
    public bool activateOnlyOnce = true;

    private bool activated = false;

    public void Init(StoneCircleManager manager, int triggerId)
    {
        this.manager = manager;
        this.triggerId = triggerId;
    }

    public void OnHit()
    {
        if (activateOnlyOnce && activated)
            return;

        activated = true;

        // 새 기계 연출 매니저가 연결되어 있으면 그걸 우선 실행
        if (activationController != null)
        {
            activationController.Activate();
            return;
        }

        // 연결 안 되어 있으면 기존 방식 유지
        if (manager != null)
        {
            manager.RotateCircles(triggerId);
        }
    }
}