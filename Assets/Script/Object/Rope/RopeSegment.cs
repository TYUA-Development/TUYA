using UnityEngine;

// 밧줄을 이루는 조각 하나. HingeJoint2D로 이전 조각(또는 앵커)과 연결되어 있다가,
// 화살이 관통하면 그 Joint를 파괴해서 그 지점부터 밧줄이 끊어진 것처럼 보이게 한다.
public class RopeSegment : MonoBehaviour, IArrowPassThrough
{
    [SerializeField] private Rope owner;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private HingeJoint2D joint;

    public Rigidbody2D Body => body;
    public bool IsCut => joint == null;

    public void Initialize(Rope owner, Rigidbody2D body, HingeJoint2D joint)
    {
        this.owner = owner;
        this.body = body;
        this.joint = joint;
    }

    public void OnArrowPass(Vector2 hitPoint, Vector2 direction)
    {
        Cut(hitPoint);
    }

    public void Cut(Vector2? cutPoint = null)
    {
        if (joint == null)
            return;

        Destroy(joint);
        joint = null;

        if (owner != null)
            owner.NotifySegmentCut(this, cutPoint ?? (Vector2)transform.position);
    }
}
