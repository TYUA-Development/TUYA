using UnityEngine;

// 밧줄 한 조각의 스프라이트와 전체 길이를 Inspector에서 입력받아,
// HingeJoint2D로 서로 이어진 여러 개의 RopeSegment 오브젝트를 만들어 한 가닥의 밧줄처럼 보이게 한다.
// 화살이 조각을 관통하면 RopeSegment가 자신의 Joint를 끊어서 밧줄이 그 지점에서 끊어지는 것처럼 만든다.
public class Rope : MonoBehaviour
{
    [Header("Rope Shape")]
    [SerializeField] private Sprite segmentSprite;
    [SerializeField] private float ropeLength = 5f;
    [SerializeField] private float segmentLength = 0.5f;
    [SerializeField] private Vector2 direction = Vector2.down;
    [SerializeField] private Transform anchor;

    [Header("Segment Physics")]
    [SerializeField] private float segmentMass = 0.1f;
    [SerializeField] private float segmentLinearDrag = 0.5f;
    [SerializeField] private float segmentAngularDrag = 0.5f;
    [SerializeField] private bool useJointLimits;
    [SerializeField] private float jointLimitAngle = 40f;

    [Header("Cut FX")]
    [SerializeField] private GameObject cutFXPrefab;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip cutClip;
    [Range(0f, 1f)]
    [SerializeField] private float cutVolume = 1f;

    [Header("Generated")]
    [SerializeField] private Transform generatedRoot;
    [SerializeField] private RopeSegment[] segments;

    private void Awake()
    {
        if (segments == null || segments.Length == 0)
            BuildRope();
    }

    private void OnValidate()
    {
        ropeLength = Mathf.Max(0.01f, ropeLength);
        segmentLength = Mathf.Max(0.01f, segmentLength);
        jointLimitAngle = Mathf.Clamp(jointLimitAngle, 0f, 179f);
    }

    public void NotifySegmentCut(RopeSegment segment, Vector2 cutPoint)
    {
        if (cutFXPrefab != null)
            Instantiate(cutFXPrefab, cutPoint, Quaternion.identity);

        if (audioSource != null && cutClip != null)
            audioSource.PlayOneShot(cutClip, cutVolume);
    }

    [ContextMenu("Build Rope")]
    public void BuildRope()
    {
        ClearRope();
        EnsureGeneratedRoot();

        Transform anchorTransform = anchor != null ? anchor : transform;
        Rigidbody2D anchorBody = EnsureAnchorRigidbody(anchorTransform);

        int segmentCount = Mathf.Max(1, Mathf.RoundToInt(ropeLength / segmentLength));
        float actualSegmentLength = ropeLength / segmentCount;

        Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.down;
        Quaternion segmentRotation = Quaternion.FromToRotation(Vector2.down, dir);

        Vector3 startPosition = anchorTransform.position;
        Rigidbody2D previousBody = anchorBody;

        segments = new RopeSegment[segmentCount];

        for (int i = 0; i < segmentCount; i++)
        {
            GameObject segmentObject = new GameObject($"RopeSegment_{i + 1:00}");
            segmentObject.transform.SetParent(generatedRoot, false);
            segmentObject.transform.position = startPosition + (Vector3)(dir * actualSegmentLength * (i + 1));
            segmentObject.transform.rotation = segmentRotation;

            SpriteRenderer renderer = segmentObject.AddComponent<SpriteRenderer>();
            renderer.sprite = segmentSprite;

            Rigidbody2D body = segmentObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.mass = segmentMass;
            body.drag = segmentLinearDrag;
            body.angularDrag = segmentAngularDrag;

            BoxCollider2D collider = segmentObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = segmentSprite != null ? segmentSprite.bounds.size : new Vector2(0.2f, actualSegmentLength);

            HingeJoint2D joint = segmentObject.AddComponent<HingeJoint2D>();
            ConfigureJoint(joint, previousBody, Vector2.up * actualSegmentLength * 0.5f, Vector2.down * actualSegmentLength * 0.5f);

            RopeSegment segment = segmentObject.AddComponent<RopeSegment>();
            segment.Initialize(this, body, joint);

            segments[i] = segment;
            previousBody = body;
        }
    }

    [ContextMenu("Clear Rope")]
    public void ClearRope()
    {
        if (generatedRoot != null)
        {
            for (int i = generatedRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = generatedRoot.GetChild(i);

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        segments = null;
    }

    private void ConfigureJoint(HingeJoint2D joint, Rigidbody2D connectedBody, Vector2 jointAnchor, Vector2 connectedAnchor)
    {
        joint.connectedBody = connectedBody;
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = jointAnchor;
        joint.connectedAnchor = connectedAnchor;
        joint.useLimits = useJointLimits;

        if (useJointLimits)
        {
            joint.limits = new JointAngleLimits2D
            {
                min = -jointLimitAngle,
                max = jointLimitAngle
            };
        }
    }

    private void EnsureGeneratedRoot()
    {
        if (generatedRoot != null)
            return;

        GameObject root = new GameObject("GeneratedRopeSegments");
        root.transform.SetParent(transform, false);
        generatedRoot = root.transform;
    }

    private Rigidbody2D EnsureAnchorRigidbody(Transform anchorTransform)
    {
        Rigidbody2D body = anchorTransform.GetComponent<Rigidbody2D>();
        if (body == null)
            body = anchorTransform.gameObject.AddComponent<Rigidbody2D>();

        body.bodyType = RigidbodyType2D.Static;
        return body;
    }
}
