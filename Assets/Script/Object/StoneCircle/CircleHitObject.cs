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

    public void Reset()
    {
        activated = false;
    }

    public void OnHit()
    {
        if (activateOnlyOnce && activated)
            return;

        activated = true;

        // �� ��� ���� �Ŵ����� ����Ǿ� ������ �װ� �켱 ����
        if (activationController != null)
        {
            activationController.Activate();
            return;
        }

        // ���� �� �Ǿ� ������ ���� ��� ����
        if (manager != null)
        {
            manager.RotateCircles(triggerId);
        }
    }
}