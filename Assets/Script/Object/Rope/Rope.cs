using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 로프에 매달 오브젝트 하나에 대한 설정. Build Rope 시 지정한 세그먼트에 HingeJoint2D로 연결된다.
[System.Serializable]
public class RopeHangingAttachment
{
    [Tooltip("밧줄에 매달 오브젝트. Rigidbody2D가 있어야 합니다.")]
    public Rigidbody2D target;

    [Tooltip("매달 세그먼트의 인덱스(0부터 시작). 음수면 맨 끝(마지막) 세그먼트에 매답니다.")]
    public int segmentIndex = -1;

    [Tooltip("세그먼트 쪽 로컬 연결점")]
    public Vector2 segmentAnchor = Vector2.zero;

    [Tooltip("매다는 오브젝트 쪽 로컬 연결점")]
    public Vector2 targetAnchor = Vector2.zero;
}

// 밧줄 한 조각의 스프라이트와 전체 길이를 Inspector에서 입력받아,
// HingeJoint2D로 서로 이어진 여러 개의 RopeSegment 오브젝트를 만들어 한 가닥의 밧줄처럼 보이게 한다.
// 화살이 조각을 관통하면 RopeSegment가 자신의 Joint를 끊어서 밧줄이 그 지점에서 끊어지는 것처럼 만든다.
public class Rope : MonoBehaviour
{
    [Header("Rope Shape")]
    [SerializeField] private Sprite segmentSprite;
    [Tooltip("세그먼트 스프라이트의 렌더링 크기 배율(콜라이더 크기도 함께 조절됨). 세그먼트 간 물리적 간격(Segment Length)에는 영향을 주지 않습니다.")]
    [SerializeField] private Vector2 segmentSpriteScale = Vector2.one;
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

    [Header("Rendering")]
    [SerializeField] private int segmentOrderInLayer = 0;

    [Tooltip("생성되는 각 RopeSegment GameObject(및 그 Visual 자식)에 적용할 유니티 레이어. 여러 레이어를 체크해도 그 중 가장 낮은 번호의 레이어 하나만 실제로 적용됩니다(GameObject는 레이어를 하나만 가질 수 있음).")]
    [SerializeField] private LayerMask segmentLayer = 1;

    [Header("Initial Settle")]
    [Tooltip("생성 직후 중력+Joint에 의해 자연스럽게 늘어진 최종 자세가 될 때까지 물리 시뮬레이션을 미리 빨리 감는다. 꺼두면 기존처럼 생성된 직선 자세에서 실제 프레임을 거치며 서서히 늘어진다.")]
    [SerializeField] private bool settleOnBuild = true;

    [Tooltip("빨리 감기할 최대 물리 스텝 수(Time.fixedDeltaTime 단위). 세그먼트 속도가 충분히 잦아들면 이보다 먼저 멈춘다.")]
    [SerializeField] private int maxSettleSteps = 120;

    [Tooltip("세그먼트(+매달린 오브젝트)의 속도가 전부 이 값 이하로 떨어지면 다 늘어진 것으로 보고 빨리 감기를 멈춘다 (유닛/초).")]
    [SerializeField] private float settleVelocityThreshold = 0.02f;

    [Header("Audio")]
    [Tooltip("세그먼트가 끊어지는 순간 한 번 재생할 효과음.")]
    [SerializeField] private AudioAssist cut_Rope;

    [Tooltip("로프가 끊어지지 않고 온전한(팽팽한) 동안 반복 재생할 소리. AudioAssist의 Loop를 켜두어야 합니다. 로프가 끊어지는 순간 멈추고, 재생성되어 다시 지어지면 다시 재생됩니다.")]
    [SerializeField] private AudioAssist loop_Rope;

    [Header("Hanging Objects")]
    [SerializeField] private RopeHangingAttachment[] hangingAttachments;

    [Header("Segment Collapse")]
    [Tooltip("끊어짐이 감지된 뒤 세그먼트가 사라지기 시작하기까지 대기하는 시간(초). Rope 스스로 IsCut을 감시해서 이 시간 뒤에 자동으로 사라집니다.")]
    [SerializeField] private float collapseDelay = 3f;
    [Tooltip("끊어진 지점에서 바깥쪽으로 한 단계(세그먼트 1~2개)씩 사라지는 간격")]
    [SerializeField] private float segmentDisappearStepDelay = 0.15f;
    [Tooltip("세그먼트 하나가 사라지는 데 걸리는 페이드아웃 시간")]
    [SerializeField] private float segmentDisappearFadeDuration = 0.1f;
    [Tooltip("체크하면(또는 코드에서 true를 대입하면) 대기 없이 즉시 세그먼트가 바깥쪽으로 순차적으로 페이드아웃되며 사라집니다. Inspector에서 직접 테스트할 수 있도록 노출되어 있으며, 트리거된 다음 프레임에 자동으로 체크가 해제됩니다.")]
    [SerializeField] private bool collapseSegments;

    [Header("Generated")]
    [SerializeField] private Transform generatedRoot;
    [SerializeField] private RopeSegment[] segments;

    private readonly List<HingeJoint2D> hangingJoints = new List<HingeJoint2D>();
    private bool isCollapsing;
    private bool isWaitingToCollapse;

    public RopeSegment[] Segments => segments;

    // CollapseSegmentsRoutine이 진행 중인지 여부. RopeRegenerator 등 외부에서 완료 시점을
    // 기다릴 때(예: 그 다음 BuildRope() 호출 전) 사용한다.
    public bool IsCollapsing => isCollapsing;

    // 세그먼트가 다 사라진 직후(재생성 여부와 무관하게) 매번 호출된다. RopeRegenerator처럼
    // "사라진 다음 무엇을 할지"를 담당하는 외부 스크립트가 이 이벤트를 구독해서 이어받는다.
    public event System.Action onCollapsed;

    // true를 대입하면(또는 Inspector에서 체크하면) 대기 없이 즉시 세그먼트가 바깥쪽으로
    // 한 단계씩 순차적으로 페이드아웃되며 사라진다. 실제 트리거는 다음 Update()에서 일어난다.
    // 별도로 아무것도 하지 않아도, Rope는 스스로 IsCut을 감시해서 collapseDelay 뒤에
    // 자동으로 이 과정을 시작한다 - 외부(RopeRegenerator 등)의 감지/트리거에 의존하지 않는다.
    public bool CollapseSegments
    {
        get => collapseSegments;
        set => collapseSegments = value;
    }

    private void Update()
    {
        if (collapseSegments)
        {
            collapseSegments = false;
            TriggerCollapse();
            return;
        }

        if (!isCollapsing && !isWaitingToCollapse && IsCut)
        {
            isWaitingToCollapse = true;

            // 끊어진 순간(붕괴가 시작되기 전, collapseDelay를 기다리는 동안 포함) 더 이상
            // 팽팽하지 않으므로 즉시 멈춘다 - 세그먼트가 실제로 사라지는 건 그보다 한참
            // 뒤(collapseDelay + 순차 페이드)라서 그때까지 기다리면 늦다.
            if (loop_Rope != null)
                loop_Rope.Stop();

            StartCoroutine(WaitThenCollapseRoutine());
        }
    }

    private IEnumerator WaitThenCollapseRoutine()
    {
        yield return new WaitForSeconds(collapseDelay);
        isWaitingToCollapse = false;
        TriggerCollapse();
    }

    private void TriggerCollapse()
    {
        if (isCollapsing)
            return;

        StartCoroutine(CollapseSegmentsRoutine());
    }

    public bool IsCut
    {
        get
        {
            if (segments == null)
                return false;

            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] != null && segments[i].IsCut)
                    return true;
            }

            return false;
        }
    }

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
        segmentSpriteScale = new Vector2(Mathf.Max(0.01f, segmentSpriteScale.x), Mathf.Max(0.01f, segmentSpriteScale.y));
    }

    public void SetHangingTarget(int attachmentIndex, Rigidbody2D newTarget)
    {
        if (hangingAttachments == null || attachmentIndex < 0 || attachmentIndex >= hangingAttachments.Length)
        {
            Debug.LogWarning($"[Rope] SetHangingTarget: attachmentIndex {attachmentIndex}가 Hanging Objects 배열 범위를 벗어났습니다 (길이={hangingAttachments?.Length ?? 0}).", this);
            return;
        }

        hangingAttachments[attachmentIndex].target = newTarget;
    }

    public void NotifySegmentCut(RopeSegment segment, Vector2 cutPoint)
    {
        if (cut_Rope != null)
            cut_Rope.Play();
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
        int resolvedSegmentLayer = ResolveSingleLayer(segmentLayer);

        for (int i = 0; i < segmentCount; i++)
        {
            GameObject segmentObject = new GameObject($"RopeSegment_{i + 1:00}");
            segmentObject.transform.SetParent(generatedRoot, false);
            segmentObject.transform.position = startPosition + (Vector3)(dir * actualSegmentLength * (i + 1));
            segmentObject.transform.rotation = segmentRotation;
            segmentObject.layer = resolvedSegmentLayer;

            GameObject visualObject = new GameObject("Visual");
            visualObject.transform.SetParent(segmentObject.transform, false);
            visualObject.transform.localScale = new Vector3(segmentSpriteScale.x, segmentSpriteScale.y, 1f);
            visualObject.layer = resolvedSegmentLayer;

            SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
            renderer.sprite = segmentSprite;
            renderer.sortingOrder = segmentOrderInLayer;

            Rigidbody2D body = segmentObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.mass = segmentMass;
            body.drag = segmentLinearDrag;
            body.angularDrag = segmentAngularDrag;

            BoxCollider2D collider = segmentObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            Vector2 baseColliderSize = segmentSprite != null ? segmentSprite.bounds.size : new Vector2(0.2f, actualSegmentLength);
            collider.size = new Vector2(baseColliderSize.x * segmentSpriteScale.x, baseColliderSize.y * segmentSpriteScale.y);

            HingeJoint2D joint = segmentObject.AddComponent<HingeJoint2D>();
            ConfigureJoint(joint, previousBody, Vector2.up * actualSegmentLength * 0.5f, Vector2.down * actualSegmentLength * 0.5f);

            RopeSegment segment = segmentObject.AddComponent<RopeSegment>();
            segment.Initialize(this, body, joint);

            segments[i] = segment;
            previousBody = body;
        }

        AttachHangingObjects();

        SettleRopePhysics();

        // 에디터에서 [ContextMenu("Build Rope")]로 플레이 모드 밖에서 호출될 수도 있는데,
        // Play()는 코루틴을 시작하므로 플레이 모드가 아니면 실행할 수 없다.
        if (Application.isPlaying && loop_Rope != null)
            loop_Rope.Play();
    }

    // 세그먼트를 직선으로 배치한 직후 그대로 두면, 실제 게임 프레임을 거치며 중력+Joint가
    // 서서히 로프를 늘어뜨리는 게 눈에 보인다(수 초간 "출렁"거리며 자리를 잡음). 그 대신
    // Physics2D.Simulate()로 물리 시간을 미리 빨리 감아서, 처음 렌더링되는 프레임부터 이미
    // 최종적으로 늘어진 자세이도록 만든다.
    //
    // Physics2D.Simulate()는 씬 전체의 물리를 진행시키는 전역 함수라, 그대로 두면 이
    // 로프와 무관한 플레이어/다른 박스 등도 이 한 프레임 안에서 실제로 몇 초치를
    // 낙하/이동해버리는 부작용이 생긴다. 그래서 로프 세그먼트(+매달린 오브젝트)를 뺀
    // 씬의 나머지 모든 Rigidbody2D는 빨리 감기 동안 Simulated를 꺼서 물리에서 완전히
    // 제외했다가 끝나면 원래대로 되돌린다.
    private void SettleRopePhysics()
    {
        if (!settleOnBuild || !Application.isPlaying || segments == null || segments.Length == 0)
            return;

        var keep = new List<Rigidbody2D>();

        foreach (RopeSegment segment in segments)
        {
            if (segment != null && segment.Body != null)
                keep.Add(segment.Body);
        }

        if (hangingAttachments != null)
        {
            foreach (RopeHangingAttachment attachment in hangingAttachments)
            {
                if (attachment != null && attachment.target != null)
                    keep.Add(attachment.target);
            }
        }

        if (keep.Count == 0)
            return;

        var keepSet = new HashSet<Rigidbody2D>(keep);
        var excluded = new List<Rigidbody2D>();

        foreach (Rigidbody2D body in FindObjectsOfType<Rigidbody2D>())
        {
            if (body == null || keepSet.Contains(body) || !body.simulated)
                continue;

            body.simulated = false;
            excluded.Add(body);
        }

        SimulationMode2D originalMode = Physics2D.simulationMode;
        Physics2D.simulationMode = SimulationMode2D.Script;

        try
        {
            float dt = Time.fixedDeltaTime;
            float stopThresholdSqr = settleVelocityThreshold * settleVelocityThreshold;

            for (int step = 0; step < maxSettleSteps; step++)
            {
                Physics2D.Simulate(dt);

                float maxSqrSpeed = 0f;
                foreach (Rigidbody2D body in keep)
                {
                    if (body == null)
                        continue;

                    float sqrSpeed = body.velocity.sqrMagnitude;
                    if (sqrSpeed > maxSqrSpeed)
                        maxSqrSpeed = sqrSpeed;
                }

                if (maxSqrSpeed <= stopThresholdSqr)
                    break;
            }
        }
        finally
        {
            Physics2D.simulationMode = originalMode;

            foreach (Rigidbody2D body in excluded)
            {
                if (body != null)
                    body.simulated = true;
            }
        }
    }

    [ContextMenu("Clear Rope")]
    public void ClearRope()
    {
        ClearHangingJoints();

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

    private void AttachHangingObjects()
    {
        ClearHangingJoints();

        if (segments == null || hangingAttachments == null)
            return;

        foreach (RopeHangingAttachment attachment in hangingAttachments)
        {
            if (attachment == null || attachment.target == null)
                continue;

            RopeSegment segment = ResolveSegment(attachment.segmentIndex);
            if (segment == null)
                continue;

            HingeJoint2D joint = attachment.target.gameObject.AddComponent<HingeJoint2D>();
            ConfigureJoint(joint, segment.Body, attachment.targetAnchor, attachment.segmentAnchor);
            hangingJoints.Add(joint);
        }
    }

    private RopeSegment ResolveSegment(int index)
    {
        if (segments == null || segments.Length == 0)
            return null;

        int resolvedIndex = index < 0 ? segments.Length - 1 : Mathf.Clamp(index, 0, segments.Length - 1);
        return segments[resolvedIndex];
    }

    private void ClearHangingJoints()
    {
        foreach (HingeJoint2D joint in hangingJoints)
        {
            if (joint == null)
                continue;

            if (Application.isPlaying)
                Destroy(joint);
            else
                DestroyImmediate(joint);
        }

        hangingJoints.Clear();
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

    // GameObject.layer는 레이어를 하나만 가질 수 있는데 Inspector 편의상 LayerMask로 받으므로,
    // 체크된 레이어들 중 가장 낮은 번호 하나를 골라 실제로 적용할 레이어로 쓴다. 아무 것도
    // 체크하지 않았으면(value == 0) 0(Default)로 대체한다.
    private static int ResolveSingleLayer(LayerMask mask)
    {
        int value = mask.value;

        for (int i = 0; i < 32; i++)
        {
            if ((value & (1 << i)) != 0)
                return i;
        }

        return 0;
    }

    // 끊어진 지점(가장 앵커에 가까운 IsCut 세그먼트)을 기준으로 양쪽으로 한 단계씩
    // 확장하며 세그먼트를 순차적으로 페이드아웃-제거한다.
    private IEnumerator CollapseSegmentsRoutine()
    {
        isCollapsing = true;

        if (segments != null && segments.Length > 0)
        {
            int pivot = FindTopmostCutIndex(segments);

            if (pivot >= 0)
            {
                foreach (List<int> step in BuildCollapseSteps(pivot, segments.Length))
                {
                    foreach (int index in step)
                    {
                        if (segments[index] != null)
                            StartCoroutine(FadeAndDestroySegment(segments[index], segmentDisappearFadeDuration));
                    }

                    yield return new WaitForSeconds(segmentDisappearStepDelay);
                }
            }
        }

        isCollapsing = false;
        onCollapsed?.Invoke();
    }

    private static int FindTopmostCutIndex(RopeSegment[] segments)
    {
        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] != null && segments[i].IsCut)
                return i;
        }

        return -1;
    }

    private static List<List<int>> BuildCollapseSteps(int pivot, int count)
    {
        var steps = new List<List<int>>();
        int upper = pivot - 1;
        int lower = pivot;

        while (upper >= 0 || lower < count)
        {
            var step = new List<int>();
            if (upper >= 0) step.Add(upper);
            if (lower < count) step.Add(lower);
            steps.Add(step);

            upper--;
            lower++;
        }

        return steps;
    }

    private IEnumerator FadeAndDestroySegment(RopeSegment segment, float duration)
    {
        if (segment == null)
            yield break;

        SpriteRenderer sr = segment.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            Color start = sr.color;
            float elapsed = 0f;
            while (elapsed < duration && sr != null)
            {
                elapsed += Time.deltaTime;
                sr.color = new Color(start.r, start.g, start.b, Mathf.Lerp(start.a, 0f, elapsed / duration));
                yield return null;
            }
        }

        if (segment != null)
            Destroy(segment.gameObject);
    }
}
