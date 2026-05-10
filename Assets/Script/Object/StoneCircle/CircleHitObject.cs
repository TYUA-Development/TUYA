using UnityEngine;

public class CircleHitObject : MonoBehaviour, IArrowHit
{
    private StoneCircleManager manager;
    private int triggerId;

    public void Init(StoneCircleManager manager, int triggerId)
    {
        this.manager = manager;
        this.triggerId = triggerId;
    }

    public void OnHit()
    {
        manager.RotateCircles(triggerId);
    }
}